import React, { useState } from 'react';
import { api } from '../api/client';
import { extractPortfolio, formatMoney, safeNum } from '../utils/formatters';
import { REFRESH_INTERVAL_SECONDS } from '../config';

export default function Dashboard({
    portfolio,
    cryptos = [],
    setActiveTab,
    lastUpdated,
    isRefreshing,
    countdown = REFRESH_INTERVAL_SECONDS,
    backendOffline,
    onRefreshMarket,
    onRefresh,
    onTradeCrypto
}) {
    const { cash, cryptoVal, total, unrealized, realized, holdings } = extractPortfolio(portfolio);

    // Close Position State
    const [closingHolding, setClosingHolding] = useState(null);
    const [isClosing, setIsClosing] = useState(false);
    const [feedback, setFeedback] = useState(null);

    const handleInitiateClose = (holding) => {
        setFeedback(null);
        setClosingHolding(holding);
    };

    const confirmClosePosition = async () => {
        if (!closingHolding) return;
        setIsClosing(true);
        try {
            const result = await api.closePosition(closingHolding.symbol);
            setFeedback({
                type: 'success',
                message: `Closed position of ${closingHolding.quantity} ${closingHolding.symbol} at $${formatMoney(result?.executionPrice || closingHolding.currentPrice)}!`
            });
            setClosingHolding(null);
            if (onRefresh) {
                await onRefresh();
            }
        } catch (err) {
            setFeedback({
                type: 'error',
                message: err.message || `Failed to close position for ${closingHolding.symbol}.`
            });
        } finally {
            setIsClosing(false);
        }
    };

    return (
        <div className="dashboard-view">
            {backendOffline && (
                <div className="alert alert-error mb-3" style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
                    <span><strong>⚠️ Backend Unreachable:</strong> Cannot connect to <code>http://localhost:5152/api</code>. Start the backend server to fetch live CoinGecko prices.</span>
                    {onRefreshMarket && (
                        <button className="btn btn-sm btn-outline" onClick={onRefreshMarket} style={{ marginLeft: '1rem' }}>
                            Retry Connection
                        </button>
                    )}
                </div>
            )}

            {feedback && (
                <div
                    className={`alert alert-${feedback.type === 'success' ? 'success' : 'error'} mb-3`}
                    style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}
                >
                    <span>{feedback.type === 'success' ? '✅' : '⚠️'} {feedback.message}</span>
                    <button
                        className="btn btn-sm btn-outline"
                        style={{ border: 'none', background: 'transparent', color: 'inherit', cursor: 'pointer' }}
                        onClick={() => setFeedback(null)}
                    >
                        ✕
                    </button>
                </div>
            )}

            {/* Quick Live Market Refresh Banner */}
            <div className="market-status-bar mb-3" style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', background: '#161b22', padding: '0.6rem 1rem', borderRadius: '6px', border: '1px solid #30363d' }}>
                <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem' }}>
                    <span className="live-indicator" style={{ display: 'inline-block', width: '8px', height: '8px', borderRadius: '50%', backgroundColor: backendOffline ? '#f85149' : '#3fb950' }}></span>
                    <span style={{ fontSize: '0.85rem', color: '#8b949e' }}>
                        {backendOffline ? 'Server Offline (Cached)' : `Live CoinGecko Market • ${REFRESH_INTERVAL_SECONDS}s Polling (Next refresh in ${countdown}s)`}
                    </span>
                    {lastUpdated && (
                        <span style={{ fontSize: '0.8rem', color: '#6e7681' }}>
                            Synced: {lastUpdated.toLocaleTimeString()}
                        </span>
                    )}
                </div>
                {onRefreshMarket && (
                    <button
                        className="btn btn-sm btn-outline"
                        onClick={onRefreshMarket}
                        disabled={isRefreshing}
                        style={{ fontSize: '0.8rem', padding: '0.2rem 0.6rem' }}
                    >
                        {isRefreshing ? 'Refreshing...' : '🔄 Refresh Prices'}
                    </button>
                )}
            </div>

            {/* Market Cards Grid */}
            <div className="market-cards-grid">
                {cryptos.map(coin => {
                    const price = safeNum(coin.currentPrice);
                    const change = safeNum(coin.priceChange24h);
                    const isPositive = change >= 0;

                    return (
                        <div key={coin.symbol} className="market-card">
                            <div className="card-top">
                                <span className="coin-symbol">{coin.symbol}</span>
                                <span className={`coin-change ${isPositive ? 'positive' : 'negative'}`}>
                                    {isPositive ? '+' : ''}{change.toFixed(2)}%
                                </span>
                            </div>
                            <div className="coin-name">{coin.name}</div>
                            <div className="coin-price">${formatMoney(price, price < 1 ? 4 : 2, price < 1 ? 6 : 2)}</div>
                            <button
                                className="btn btn-sm btn-outline"
                                onClick={() => {
                                    if (onTradeCrypto) onTradeCrypto(coin.symbol);
                                    else setActiveTab('trading');
                                }}
                            >
                                Trade {coin.symbol}
                            </button>
                        </div>
                    );
                })}
            </div>

            {/* Financial Overview Cards */}
            <div className="section-header mt-4">
                <h3>Portfolio Summary</h3>
            </div>

            <div className="metrics-grid">
                <div className="metric-card metric-primary">
                    <div className="metric-label">Total Portfolio Net Worth</div>
                    <div className="metric-value">${formatMoney(total)}</div>
                    <div className="metric-sub">Mark-to-Market Total</div>
                </div>

                <div className="metric-card">
                    <div className="metric-label">Available Cash (USD)</div>
                    <div className="metric-value">${formatMoney(cash)}</div>
                    <button className="btn-link-action" onClick={() => setActiveTab('funds')}>
                        + Deposit / Withdraw
                    </button>
                </div>

                <div className="metric-card">
                    <div className="metric-label">Crypto Holdings Value</div>
                    <div className="metric-value">${formatMoney(cryptoVal)}</div>
                    <div className="metric-sub">{holdings.length} Active Asset{holdings.length !== 1 ? 's' : ''}</div>
                </div>

                <div className="metric-card">
                    <div className="metric-label">Unrealized P/L</div>
                    <div className={`metric-value ${unrealized >= 0 ? 'text-success' : 'text-danger'}`}>
                        {unrealized >= 0 ? '+' : ''}${formatMoney(unrealized)}
                    </div>
                    <div className="metric-sub">Open Positions</div>
                </div>

                <div className="metric-card">
                    <div className="metric-label">Total Realized P/L</div>
                    <div className={`metric-value ${realized >= 0 ? 'text-success' : 'text-danger'}`}>
                        {realized >= 0 ? '+' : ''}${formatMoney(realized)}
                    </div>
                    <div className="metric-sub">Closed Trades</div>
                </div>
            </div>

            {/* Market Overview Table */}
            <div className="card mt-4">
                <div className="card-header">
                    <h4>Market Overview (Top Cryptocurrencies)</h4>
                    <button className="btn btn-sm btn-outline" onClick={() => setActiveTab('trading')}>
                        Go to Trading Room
                    </button>
                </div>
                <div className="table-responsive">
                    <table className="table">
                        <thead>
                            <tr>
                                <th>Cryptocurrency</th>
                                <th>Symbol</th>
                                <th>Current Price</th>
                                <th>24h Change</th>
                                <th>Action</th>
                            </tr>
                        </thead>
                        <tbody>
                            {cryptos.length === 0 ? (
                                <tr>
                                    <td colSpan="5" className="text-center text-muted">Loading live cryptocurrency market prices...</td>
                                </tr>
                            ) : (
                                cryptos.map(c => {
                                    const change = safeNum(c.priceChange24h);
                                    const price = safeNum(c.currentPrice);
                                    return (
                                        <tr key={c.symbol}>
                                            <td>
                                                <strong>{c.name}</strong>
                                            </td>
                                            <td>
                                                <span className="badge badge-info">{c.symbol}</span>
                                            </td>
                                            <td>
                                                <strong>${formatMoney(price, price < 1 ? 4 : 2, price < 1 ? 4 : 2)}</strong>
                                            </td>
                                            <td className={change >= 0 ? 'text-success' : 'text-danger'}>
                                                {change >= 0 ? '+' : ''}{change.toFixed(2)}%
                                            </td>
                                            <td>
                                                <button
                                                    className="btn btn-sm btn-primary"
                                                    onClick={() => {
                                                        if (onTradeCrypto) onTradeCrypto(c.symbol);
                                                        else setActiveTab('trading');
                                                    }}
                                                >
                                                    Trade {c.symbol}
                                                </button>
                                            </td>
                                        </tr>
                                    );
                                })
                            )}
                        </tbody>
                    </table>
                </div>
            </div>

            {/* Current Holdings Table */}
            <div className="card mt-4">
                <div className="card-header">
                    <h4>Current Holdings</h4>
                    <button className="btn btn-sm btn-primary" onClick={() => setActiveTab('portfolio')}>
                        View Full Portfolio & Statements
                    </button>
                </div>
                <div className="table-responsive">
                    <table className="table">
                        <thead>
                            <tr>
                                <th>Asset</th>
                                <th>Quantity</th>
                                <th>Avg Buy Price</th>
                                <th>Current Price</th>
                                <th>Market Value</th>
                                <th>Unrealized P/L</th>
                                <th style={{ minWidth: '150px' }}>Action</th>
                            </tr>
                        </thead>
                        <tbody>
                            {holdings.length === 0 ? (
                                <tr>
                                    <td colSpan="7" className="text-center text-muted" style={{ padding: '1.5rem' }}>
                                        No cryptocurrency holdings yet. Place your first trade!
                                    </td>
                                </tr>
                            ) : (
                                holdings.map(h => {
                                    const pnl = h.unrealizedProfitLoss;
                                    const pnlPct = h.unrealizedProfitLossPercentage;
                                    return (
                                        <tr key={h.symbol}>
                                            <td><strong>{h.symbol}</strong> <small className="text-muted">({h.name})</small></td>
                                            <td>{h.quantity.toFixed(8)}</td>
                                            <td>${formatMoney(h.averageCost)}</td>
                                            <td>${formatMoney(h.currentPrice, h.currentPrice < 1 ? 4 : 2, h.currentPrice < 1 ? 4 : 2)}</td>
                                            <td><strong>${formatMoney(h.currentValue)}</strong></td>
                                            <td className={pnl >= 0 ? 'text-success' : 'text-danger'}>
                                                {pnl >= 0 ? '+' : ''}${formatMoney(pnl)} ({pnl >= 0 ? '+' : ''}{pnlPct.toFixed(2)}%)
                                            </td>
                                            <td>
                                                <div style={{ display: 'flex', gap: '0.4rem' }}>
                                                    <button
                                                        className="btn btn-sm btn-outline"
                                                        onClick={() => {
                                                            if (onTradeCrypto) onTradeCrypto(h.symbol);
                                                            else setActiveTab('trading');
                                                        }}
                                                    >
                                                        Trade
                                                    </button>
                                                    <button
                                                        className="btn btn-sm"
                                                        style={{
                                                            background: 'rgba(239, 68, 68, 0.15)',
                                                            color: '#f87171',
                                                            border: '1px solid rgba(239, 68, 68, 0.3)',
                                                            cursor: 'pointer'
                                                        }}
                                                        onClick={() => handleInitiateClose(h)}
                                                        title={`Close entire position of ${h.quantity} ${h.symbol}`}
                                                    >
                                                        Close
                                                    </button>
                                                </div>
                                            </td>
                                        </tr>
                                    );
                                })
                            )}
                        </tbody>
                    </table>
                </div>
            </div>

            {/* Confirmation Modal for One-Click Close Position */}
            {closingHolding && (
                <div
                    className="modal-backdrop"
                    style={{
                        position: 'fixed',
                        top: 0,
                        left: 0,
                        right: 0,
                        bottom: 0,
                        backgroundColor: 'rgba(0, 0, 0, 0.75)',
                        display: 'flex',
                        alignItems: 'center',
                        justifyContent: 'center',
                        zIndex: 9999,
                        backdropFilter: 'blur(3px)'
                    }}
                >
                    <div
                        className="card p-4"
                        style={{
                            maxWidth: '460px',
                            width: '90%',
                            background: '#161b22',
                            border: '1px solid rgba(239, 68, 68, 0.35)',
                            borderRadius: '10px',
                            boxShadow: '0 20px 25px -5px rgba(0, 0, 0, 0.6)'
                        }}
                    >
                        <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', marginBottom: '1rem' }}>
                            <span style={{ fontSize: '1.5rem' }}>⚠️</span>
                            <h4 style={{ margin: 0, color: '#f87171' }}>Close Position: {closingHolding.symbol}</h4>
                        </div>

                        <p style={{ color: '#c9d1d9', fontSize: '0.92rem', lineHeight: '1.4', marginBottom: '1rem' }}>
                            Are you sure you want to completely liquidate your entire position in <strong>{closingHolding.symbol}</strong> ({closingHolding.name})?
                        </p>

                        <div
                            style={{
                                background: '#0d1117',
                                border: '1px solid #30363d',
                                borderRadius: '8px',
                                padding: '0.85rem 1rem',
                                marginBottom: '1.25rem',
                                fontSize: '0.88rem'
                            }}
                        >
                            <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '0.4rem' }}>
                                <span className="text-muted">Quantity to Liquidate:</span>
                                <strong>{closingHolding.quantity} {closingHolding.symbol}</strong>
                            </div>
                            <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '0.4rem' }}>
                                <span className="text-muted">Current Execution Price:</span>
                                <span>${formatMoney(closingHolding.currentPrice, closingHolding.currentPrice < 1 ? 4 : 2, closingHolding.currentPrice < 1 ? 4 : 2)}</span>
                            </div>
                            <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '0.4rem' }}>
                                <span className="text-muted">Estimated Cash Proceeds:</span>
                                <strong style={{ color: '#58a6ff' }}>${formatMoney(closingHolding.currentValue)}</strong>
                            </div>
                            <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                                <span className="text-muted">Unrealized P/L to Realize:</span>
                                <span className={closingHolding.unrealizedProfitLoss >= 0 ? 'text-success' : 'text-danger'} style={{ fontWeight: 600 }}>
                                    {closingHolding.unrealizedProfitLoss >= 0 ? '+' : ''}${formatMoney(closingHolding.unrealizedProfitLoss)} ({closingHolding.unrealizedProfitLossPercentage.toFixed(2)}%)
                                </span>
                            </div>
                        </div>

                        <p className="text-muted small" style={{ marginBottom: '1.25rem' }}>
                            💡 <strong>No quantity entry required:</strong> This executes an immediate MARKET SELL order for 100% of your position. The settlement proceeds will be credited instantly to your cash balance.
                        </p>

                        <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '0.75rem' }}>
                            <button
                                className="btn btn-outline"
                                onClick={() => setClosingHolding(null)}
                                disabled={isClosing}
                            >
                                Cancel
                            </button>
                            <button
                                className="btn"
                                style={{
                                    background: '#da3633',
                                    borderColor: '#f85149',
                                    color: '#ffffff',
                                    fontWeight: 600
                                }}
                                onClick={confirmClosePosition}
                                disabled={isClosing}
                            >
                                {isClosing ? 'Liquidating Position...' : 'Confirm & Close Position'}
                            </button>
                        </div>
                    </div>
                </div>
            )}
        </div>
    );
}
