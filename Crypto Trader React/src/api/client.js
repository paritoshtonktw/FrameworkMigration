/**
 * Centralized API Client for Crypto Trader React
 * Communicates with the ASP.NET Core backend at http://localhost:5152/api
 */

import { API_BASE_URL } from '../config';

/**
 * Parses a JWT payload safely without external dependencies.
 */
export function parseJwt(token) {
    if (!token || typeof token !== 'string') return null;
    const parts = token.split('.');
    if (parts.length !== 3) return null;
    try {
        const base64 = parts[1].replace(/-/g, '+').replace(/_/g, '/');
        const pad = base64.length % 4;
        const padded = pad ? base64 + '='.repeat(4 - pad) : base64;
        const decoded = atob(padded);
        const utf8 = decodeURIComponent(
            decoded
                .split('')
                .map(c => '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2))
                .join('')
        );
        return JSON.parse(utf8);
    } catch {
        try {
            return JSON.parse(atob(parts[1].replace(/-/g, '+').replace(/_/g, '/')));
        } catch {
            return null;
        }
    }
}

/**
 * Checks if a JWT token is expired based on its 'exp' claim.
 */
export function isTokenExpired(token) {
    if (!token) return true;
    const payload = parseJwt(token);
    if (!payload || !payload.exp) return false;
    // exp is in seconds; Date.now() is in milliseconds
    return payload.exp * 1000 <= Date.now();
}

let sessionExpiredListeners = [];

export const authStorage = {
    getToken: () => localStorage.getItem('crypto_trader_token'),
    setToken: (token) => localStorage.setItem('crypto_trader_token', token),
    clearToken: () => localStorage.removeItem('crypto_trader_token'),
    getUser: () => {
        try {
            const token = authStorage.getToken();
            // Invalidate immediately if token is missing or expired
            if (!token || isTokenExpired(token)) {
                authStorage.logout();
                return null;
            }
            return JSON.parse(localStorage.getItem('crypto_trader_user') || 'null');
        } catch {
            return null;
        }
    },
    setUser: (user) => localStorage.setItem('crypto_trader_user', JSON.stringify(user)),
    clearUser: () => localStorage.removeItem('crypto_trader_user'),
    logout: () => {
        authStorage.clearToken();
        authStorage.clearUser();
    },
    isTokenExpired: () => isTokenExpired(authStorage.getToken()),
    onSessionExpired: (listener) => {
        sessionExpiredListeners.push(listener);
        return () => {
            sessionExpiredListeners = sessionExpiredListeners.filter(l => l !== listener);
        };
    },
    notifySessionExpired: (reason = 'expired') => {
        authStorage.logout();
        sessionExpiredListeners.forEach(listener => {
            try {
                listener(reason);
            } catch (e) {
                console.error('Error in session expiration listener:', e);
            }
        });
        window.dispatchEvent(new CustomEvent('crypto_trader:session_expired', { detail: { reason } }));
    }
};

async function request(endpoint, options = {}) {
    const url = `${API_BASE_URL}${endpoint}`;
    const headers = {
        'Content-Type': 'application/json',
        'Accept': 'application/json',
        ...(options.headers || {})
    };

    const isAuthEndpoint = endpoint.startsWith('/auth/login') || endpoint.startsWith('/auth/register');
    const token = authStorage.getToken();

    if (token) {
        // Proactive expiration check: if token has expired before initiating authenticated request
        if (!isAuthEndpoint && isTokenExpired(token)) {
            authStorage.notifySessionExpired('expired');
            const error = new Error('Your session has expired. Please sign in again.');
            error.status = 401;
            error.errorCode = 'TOKEN_EXPIRED';
            throw error;
        }
        headers['Authorization'] = `Bearer ${token}`;
    }

    const response = await fetch(url, {
        cache: 'no-store',
        ...options,
        headers
    });

    // Reactive 401 handling (e.g. server rejected token or token expired mid-session)
    if (response.status === 401 && !isAuthEndpoint) {
        authStorage.notifySessionExpired('unauthorized');
        let data = null;
        try {
            data = await response.json();
        } catch {
            // Ignore JSON parse failure on raw error responses
        }
        const errorMsg = data?.message || 'Your session has expired. Please sign in again.';
        const error = new Error(errorMsg);
        error.status = 401;
        error.errorCode = data?.errorCode || 'UNAUTHORIZED';
        throw error;
    }

    const data = await response.json();

    if (!response.ok || !data.success) {
        const errorMsg = data.message || `Request failed with status ${response.status}`;
        const error = new Error(errorMsg);
        error.status = response.status;
        error.errorCode = data.errorCode;
        throw error;
    }

    return data.data;
}

export const api = {
    // Auth
    login: async (usernameOrEmail, password) => {
        const res = await request('/auth/login', {
            method: 'POST',
            body: JSON.stringify({ usernameOrEmail, password })
        });
        if (res.token) {
            authStorage.setToken(res.token);
            authStorage.setUser(res.user);
        }
        return res;
    },

    register: async (payload) => {
        const res = await request('/auth/register', {
            method: 'POST',
            body: JSON.stringify(payload)
        });
        if (res.token) {
            authStorage.setToken(res.token);
            authStorage.setUser(res.user);
        }
        return res;
    },

    // Market / Cryptocurrencies
    getCryptos: (force = false) => request(`/cryptocurrencies${force ? '?force=true' : ''}`),
    getCrypto: (symbol) => request(`/cryptocurrencies/${symbol}`),
    getCryptoChart: (symbol, timeframe = '24h') => request(`/cryptocurrencies/${symbol}/chart?timeframe=${encodeURIComponent(timeframe)}`),

    // Portfolio
    getPortfolio: () => request('/portfolio'),
    closePosition: (symbol) => request(`/portfolio/positions/${symbol}/close`, { method: 'POST' }),
    downloadPnLReport: async (timeframe = '30d') => {
        const token = authStorage.getToken();
        if (token && isTokenExpired(token)) {
            authStorage.notifySessionExpired('expired');
            throw new Error('Your session has expired. Please sign in again.');
        }
        const res = await fetch(`${API_BASE_URL}/reports/pnl-settlement?timeframe=${encodeURIComponent(timeframe)}`, {
            headers: token ? { 'Authorization': `Bearer ${token}` } : {}
        });
        if (res.status === 401) {
            authStorage.notifySessionExpired('unauthorized');
            throw new Error('Your session has expired. Please sign in again.');
        }
        if (!res.ok) {
            throw new Error(`Failed to generate report (${res.status})`);
        }
        const blob = await res.blob();
        const blobUrl = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = blobUrl;
        a.download = `Crypto_PnL_Settlement_Report_${timeframe}_${new Date().toISOString().slice(0, 10)}.pdf`;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        setTimeout(() => window.URL.revokeObjectURL(blobUrl), 1000);
    },

    // Orders
    getOrders: (status) => {
        const query = status ? `?status=${encodeURIComponent(status)}` : '';
        return request(`/orders${query}`);
    },
    createOrder: (order) => request('/orders', {
        method: 'POST',
        body: JSON.stringify(order)
    }),
    cancelOrder: (orderId) => request(`/orders/${orderId}`, {
        method: 'DELETE'
    }),

    // Trades
    getTrades: () => request('/trades'),

    // Ledger / Transactions
    getTransactions: () => request('/transactions'),

    // Funds
    getDeposits: () => request('/deposits'),
    createDeposit: (amount, currency = 'USD') => request('/deposits', {
        method: 'POST',
        body: JSON.stringify({ amount: parseFloat(amount), currency })
    }),
    getWithdrawals: () => request('/withdrawals'),
    createWithdrawal: (amount, currency = 'USD') => request('/withdrawals', {
        method: 'POST',
        body: JSON.stringify({ amount: parseFloat(amount), currency })
    }),

    // Profile
    getProfile: () => request('/profile'),
    updateProfile: (profile) => request('/profile', {
        method: 'PUT',
        body: JSON.stringify(profile)
    })
};

export default api;

