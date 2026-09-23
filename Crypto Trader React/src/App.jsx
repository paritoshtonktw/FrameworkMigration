import React, { useState, useEffect, useCallback } from 'react';
import { api, authStorage, isTokenExpired } from './api/client';
import Navbar from './components/Navbar';
import Login from './components/Login';
import Register from './components/Register';
import Dashboard from './components/Dashboard';
import Trading from './components/Trading';
import Portfolio from './components/Portfolio';
import Orders from './components/Orders';
import Trades from './components/Trades';
import Ledger from './components/Ledger';
import Funds from './components/Funds';
import Profile from './components/Profile';
import { REFRESH_INTERVAL_SECONDS, REFRESH_INTERVAL_MS, IS_LEGACY_BACKEND, BACKEND_LABEL_LONG } from './config';

export default function App() {
    const [user, setUser] = useState(() => authStorage.getUser());
    const [authMode, setAuthMode] = useState('login'); // 'login' or 'register'
    const [sessionExpiredMsg, setSessionExpiredMsg] = useState('');
    const [activeTab, setActiveTab] = useState('dashboard');
    const [selectedSymbol, setSelectedSymbol] = useState('BTC');
    const [cryptos, setCryptos] = useState([]);
    const [portfolio, setPortfolio] = useState(null);
    const [lastMarketUpdate, setLastMarketUpdate] = useState(null);
    const [isRefreshingMarket, setIsRefreshingMarket] = useState(false);
    const [marketRefreshCountdown, setMarketRefreshCountdown] = useState(REFRESH_INTERVAL_SECONDS);
    const [backendOffline, setBackendOffline] = useState(false);

    const fetchMarketData = useCallback(async (force = false) => {
        setIsRefreshingMarket(true);
        try {
            const data = await api.getCryptos(force);
            setCryptos(data || []);
            setLastMarketUpdate(new Date());
            setBackendOffline(false);
        } catch (err) {
            console.error('Failed to load cryptos:', err);
            setBackendOffline(true);
        } finally {
            setIsRefreshingMarket(false);
            setMarketRefreshCountdown(REFRESH_INTERVAL_SECONDS);
        }
    }, []);

    const fetchPortfolioData = useCallback(async () => {
        if (!user) return;
        try {
            const data = await api.getPortfolio();
            setPortfolio(data);
            setBackendOffline(false);
        } catch (err) {
            console.error('Failed to load portfolio:', err);
        }
    }, [user]);

    const refreshMarket = useCallback(async (force = false) => {
        await fetchMarketData(force);
        if (user) {
            await fetchPortfolioData();
        }
    }, [fetchMarketData, fetchPortfolioData, user]);

    // Initial load
    useEffect(() => {
        fetchMarketData();
    }, [fetchMarketData]);

    // 1-second interval countdown for market refresh
    useEffect(() => {
        const timer = setInterval(() => {
            setMarketRefreshCountdown(prev => {
                if (prev <= 1) {
                    fetchMarketData();
                    if (user) {
                        fetchPortfolioData();
                    }
                    return REFRESH_INTERVAL_SECONDS;
                }
                return prev - 1;
            });
        }, 1000);
        return () => clearInterval(timer);
    }, [fetchMarketData, fetchPortfolioData, user]);

    useEffect(() => {
        if (user) {
            fetchPortfolioData();
            const portTimer = setInterval(fetchPortfolioData, REFRESH_INTERVAL_MS);
            return () => clearInterval(portTimer);
        } else {
            setPortfolio(null);
        }
    }, [user, fetchPortfolioData]);

    const handleTradeCrypto = (symbol) => {
        setSelectedSymbol(symbol || 'BTC');
        setActiveTab('trading');
    };

    const handleLoginSuccess = (loggedInUser) => {
        setSessionExpiredMsg('');
        setUser(loggedInUser);
        setActiveTab('dashboard');
    };

    const handleLogout = useCallback((reason = null) => {
        authStorage.logout();
        setUser(null);
        setPortfolio(null);
        setAuthMode('login');
        if (reason) {
            setSessionExpiredMsg('Your session has expired. Please sign in again.');
        } else {
            setSessionExpiredMsg('');
        }
    }, []);

    // Listen for session expiration events (from 401s or proactive checks in client.js)
    useEffect(() => {
        const unsubscribe = authStorage.onSessionExpired((reason) => {
            handleLogout(reason || 'expired');
        });
        const handleCustomEvent = (e) => {
            handleLogout(e?.detail?.reason || 'expired');
        };
        window.addEventListener('crypto_trader:session_expired', handleCustomEvent);
        return () => {
            if (unsubscribe) unsubscribe();
            window.removeEventListener('crypto_trader:session_expired', handleCustomEvent);
        };
    }, [handleLogout]);

    // Periodically verify token expiration while user is logged in
    useEffect(() => {
        if (!user) return;
        const checkExpiry = () => {
            const token = authStorage.getToken();
            if (!token || isTokenExpired(token)) {
                handleLogout('expired');
            }
        };
        checkExpiry();
        const interval = setInterval(checkExpiry, 5000);
        return () => clearInterval(interval);
    }, [user, handleLogout]);

    if (!user) {
        return (
            <div className="auth-wrapper">
                <div className="auth-container">
                    <div className="auth-brand">
                        <span className="brand-logo">⚡</span>
                        <h1>CryptoTrader</h1>
                        <span className={IS_LEGACY_BACKEND ? "badge-legacy" : "badge-uplifted"}>
                            {BACKEND_LABEL_LONG}
                        </span>
                    </div>

                    {authMode === 'login' ? (
                        <Login
                            onLoginSuccess={handleLoginSuccess}
                            onSwitchToRegister={() => {
                                setSessionExpiredMsg('');
                                setAuthMode('register');
                            }}
                            sessionExpiredMessage={sessionExpiredMsg}
                        />
                    ) : (
                        <Register
                            onRegisterSuccess={handleLoginSuccess}
                            onSwitchToLogin={() => {
                                setSessionExpiredMsg('');
                                setAuthMode('login');
                            }}
                        />
                    )}
                </div>
            </div>
        );
    }

    return (
        <div className="app-container">
            <Navbar
                activeTab={activeTab}
                setActiveTab={setActiveTab}
                user={user}
                portfolio={portfolio}
                onLogout={handleLogout}
            />

            <main className="main-content">
                {activeTab === 'dashboard' && (
                    <Dashboard
                        portfolio={portfolio}
                        cryptos={cryptos}
                        setActiveTab={setActiveTab}
                        lastUpdated={lastMarketUpdate}
                        isRefreshing={isRefreshingMarket}
                        countdown={marketRefreshCountdown}
                        backendOffline={backendOffline}
                        onRefreshMarket={() => refreshMarket(true)}
                        onRefresh={() => refreshMarket(true)}
                        onTradeCrypto={handleTradeCrypto}
                    />
                )}

                {activeTab === 'trading' && (
                    <Trading
                        cryptos={cryptos}
                        portfolio={portfolio}
                        selectedSymbol={selectedSymbol}
                        onSelectSymbol={setSelectedSymbol}
                        lastUpdated={lastMarketUpdate}
                        isRefreshing={isRefreshingMarket}
                        countdown={marketRefreshCountdown}
                        backendOffline={backendOffline}
                        onRefreshMarket={() => refreshMarket(true)}
                        onTradeExecuted={() => {
                            fetchPortfolioData();
                            fetchMarketData(true);
                        }}
                    />
                )}

                {activeTab === 'portfolio' && (
                    <Portfolio
                        portfolio={portfolio}
                        setActiveTab={setActiveTab}
                        lastUpdated={lastMarketUpdate}
                        isRefreshing={isRefreshingMarket}
                        onRefresh={() => refreshMarket(true)}
                        onTradeCrypto={handleTradeCrypto}
                    />
                )}

                {activeTab === 'orders' && (
                    <Orders
                        onOrderUpdated={() => {
                            fetchPortfolioData();
                        }}
                    />
                )}

                {activeTab === 'trades' && (
                    <Trades />
                )}

                {activeTab === 'ledger' && (
                    <Ledger />
                )}

                {activeTab === 'funds' && (
                    <Funds
                        portfolio={portfolio}
                        onFundsUpdated={() => {
                            fetchPortfolioData();
                        }}
                    />
                )}

                {activeTab === 'profile' && (
                    <Profile
                        user={user}
                        onProfileUpdated={(updatedUser) => {
                            setUser(prev => ({ ...prev, ...updatedUser }));
                            authStorage.setUser({ ...user, ...updatedUser });
                        }}
                    />
                )}
            </main>
        </div>
    );
}
