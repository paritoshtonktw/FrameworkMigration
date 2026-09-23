import React from 'react';
import { extractPortfolio, formatMoney } from '../utils/formatters';
import { IS_LEGACY_BACKEND, BACKEND_LABEL } from '../config';

export default function Navbar({ activeTab, setActiveTab, user, portfolio, onLogout }) {
    const { cash, total } = extractPortfolio(portfolio);
    const cashFormatted = formatMoney(cash);
    const totalFormatted = formatMoney(total);

    return (
        <header className="navbar">
            <div className="navbar-brand">
                <span className="logo-icon">⚡</span>
                <span className="brand-title">CryptoTrader</span>
                <span className={IS_LEGACY_BACKEND ? "badge-legacy" : "badge-uplifted"}>
                    {BACKEND_LABEL}
                </span>
            </div>

            <nav className="navbar-nav">
                <button className={`nav-btn ${activeTab === 'dashboard' ? 'active' : ''}`} onClick={() => setActiveTab('dashboard')}>
                    Dashboard
                </button>
                <button className={`nav-btn ${activeTab === 'trading' ? 'active' : ''}`} onClick={() => setActiveTab('trading')}>
                    Trading
                </button>
                <button className={`nav-btn ${activeTab === 'portfolio' ? 'active' : ''}`} onClick={() => setActiveTab('portfolio')}>
                    Portfolio
                </button>
                <button className={`nav-btn ${activeTab === 'orders' ? 'active' : ''}`} onClick={() => setActiveTab('orders')}>
                    Orders
                </button>
                <button className={`nav-btn ${activeTab === 'trades' ? 'active' : ''}`} onClick={() => setActiveTab('trades')}>
                    Trades
                </button>
                <button className={`nav-btn ${activeTab === 'ledger' ? 'active' : ''}`} onClick={() => setActiveTab('ledger')}>
                    Ledger
                </button>
                <button className={`nav-btn ${activeTab === 'funds' ? 'active' : ''}`} onClick={() => setActiveTab('funds')}>
                    Funds
                </button>
                <button className={`nav-btn ${activeTab === 'profile' ? 'active' : ''}`} onClick={() => setActiveTab('profile')}>
                    Profile
                </button>
            </nav>

            <div className="navbar-user">
                <div className="user-balances">
                    <span className="balance-item">
                        <small>Cash:</small> <strong>${cashFormatted}</strong>
                    </span>
                    <span className="balance-item">
                        <small>Net Worth:</small> <strong className="highlight">${totalFormatted}</strong>
                    </span>
                </div>
                <div className="user-profile-badge">
                    <span className="user-name" title={user?.username ? `@${user.username}` : ''}>
                        👤 {user?.firstName ? `${user.firstName} ${user.lastName || ''}`.trim() : (user?.username || 'Trader')}
                    </span>
                    <button className="btn-logout" onClick={onLogout} title="Sign Out">
                        Sign Out
                    </button>
                </div>
            </div>
        </header>
    );
}

