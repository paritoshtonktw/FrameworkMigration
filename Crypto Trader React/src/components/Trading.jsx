import React, { useState, useEffect } from 'react';
import { api } from '../api/client';
import { extractPortfolio, formatMoney, safeNum } from '../utils/formatters';
import CryptoChart from './CryptoChart';
import { REFRESH_INTERVAL_SECONDS } from '../config';

export default function Trading({
    cryptos = [],
    portfolio,
    selectedSymbol: incomingSymbol,
    onSelectSymbol,
    lastUpdated,
    isRefreshing,
    countdown = REFRESH_INTERVAL_SECONDS,
    backendOffline,
    onRefreshMarket,
    onTradeExecuted
}) {
    const [selectedSymbol, setSelectedSymbol] = useState(incomingSymbol || 'BTC');
    const [side, setSide] = useState('BUY'); // BUY, SELL
    const [orderType, setOrderType] = useState('MARKET'); // MARKET, LIMIT
    const [quantity, setQuantity] = useState('');
    const [limitPrice, setLimitPrice] = useState('');
    const [loading, setLoading] = useState(false);
    const [message, setMessage] = useState(null);

    // Synchronize with external symbol selection (e.g. from Dashboard or URL)
    useEffect(() => {
        if (incomingSymbol && incomingSymbol !== selectedSymbol) {
            setSelectedSymbol(incomingSymbol);
        }
    }, [incomingSymbol, selectedSymbol]);

    const handleSelectSymbol = (sym) => {
        setSelectedSymbol(sym);
        if (onSelectSymbol) {
            onSelectSymbol(sym);
        }
    };

    const activeCrypto = cryptos.find(c => c.symbol === selectedSymbol) || cryptos[0] || {};
    const livePrice = safeNum(activeCrypto.currentPrice);

    // Current holdings for selected coin
    const { cash: availableCash, holdings } = extractPortfolio(portfolio);
    const currentHolding = holdings.find(h => h.symbol === selectedSymbol);
    const availableCryptoQty = currentHolding ? currentHolding.quantity : 0;

    const currentTradePrice = orderType === 'LIMIT' && limitPrice ? safeNum(limitPrice) : livePrice;
    const estimatedTotal = (safeNum(quantity) || 0) * currentTradePrice;

    // Reset or update limit price target when active coin changes
    useEffect(() => {
        if (orderType === 'LIMIT' && livePrice > 0 && !limitPrice) {
            setLimitPrice(livePrice.toFixed(livePrice < 1 ? 4 : 2));
        }
    }, [selectedSymbol, orderType, livePrice, limitPrice]);

    const handleQuickPercent = (pct) => {
        if (side === 'BUY') {
            if (currentTradePrice <= 0 || isNaN(currentTradePrice)) return;
            const maxBuyAmount = availableCash * pct;
            const qty = (maxBuyAmount / currentTradePrice).toFixed(6);
            setQuantity(isNaN(Number(qty)) ? '0.00' : qty);
        } else {
            const qty = (availableCryptoQty * pct).toFixed(6);
            setQuantity(isNaN(Number(qty)) ? '0.00' : qty);
        }
    };

    const handleExecuteOrder = async (e) => {
        e.preventDefault();
        setMessage(null);
        const qtyNum = parseFloat(quantity);
        if (!qtyNum || qtyNum <= 0 || isNaN(qtyNum)) {
            setMessage({ type: 'error', text: 'Please enter a valid positive quantity.' });
            return;
        }

        if (side === 'BUY' && estimatedTotal > availableCash) {
            setMessage({
                type: 'error',
                text: `Insufficient cash balance. Required: $${formatMoney(estimatedTotal)}, Available: $${formatMoney(availableCash)}`
            });
            return;
        }

        if (side === 'SELL' && qtyNum > availableCryptoQty) {
            setMessage({
                type: 'error',
                text: `Insufficient ${selectedSymbol} balance. Required: ${qtyNum}, Available: ${availableCryptoQty.toFixed(8)}`
            });
            return;
        }

        setLoading(true);
        try {
            const payload = {
                symbol: selectedSymbol,
                orderType,
                side,
                quantity: qtyNum,
                price: orderType === 'LIMIT' ? parseFloat(limitPrice) : null
            };

            const orderResult = await api.createOrder(payload);
            const execP = safeNum(orderResult.executedPrice ?? orderResult.executionPrice ?? orderResult.price ?? livePrice);
            const status = orderResult.status || 'FILLED';
            setMessage({
                type: 'success',
                text: `Successfully placed ${side} order for ${qtyNum} ${selectedSymbol}! (Status: ${status} @ $${formatMoney(execP, execP < 1 ? 4 : 2, execP < 1 ? 6 : 2)})`
            });
            setQuantity('');
            if (onTradeExecuted) onTradeExecuted();
        } catch (err) {
            setMessage({ type: 'error', text: err.message || 'Order execution failed.' });
        } finally {
            setLoading(false);
        }
    };

    return (
        <div className="trading-view">
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

            <div className="trading-layout">
                {/* ========================================================================= */}
                {/* LEFT BAR: Cryptocurrency Watchlist & Order Entry Ticket                  */}
                {/* ========================================================================= */}
                <div className="trading-left-bar">
                    {/* Cryptocurrency Watchlist */}
                    <div className="card crypto-watchlist-card mb-3">
                        <div className="card-header">
                            <h4>Cryptocurrencies</h4>
                            <span className="badge badge-secondary">{cryptos.length} Assets</span>
                        </div>
                        <div className="crypto-watchlist-list">
                            {cryptos.map(c => {
                                const p = safeNum(c.currentPrice);
                                const chg = safeNum(c.priceChange24h);
                                const isSelected = c.symbol === selectedSymbol;
                                return (
                                    <div
                                        key={c.symbol}
                                        className={`watchlist-item ${isSelected ? 'active' : ''}`}
                                        onClick={() => handleSelectSymbol(c.symbol)}
                                    >
                                        <div className="watchlist-item-left">
                                            <div className="watchlist-symbol-row">
                                                <span className="watchlist-symbol">{c.symbol}</span>
                                                {isSelected && <span className="watchlist-active-dot">●</span>}
                                            </div>
                                            <span className="watchlist-name">{c.name}</span>
                                        </div>
                                        <div className="watchlist-item-right">
                                            <span className="watchlist-price">
                                                ${formatMoney(p, p < 1 ? 4 : 2, p < 1 ? 4 : 2)}
                                            </span>
                                            <span className={`watchlist-change ${chg >= 0 ? 'positive' : 'negative'}`}>
                                                {chg >= 0 ? '+' : ''}{chg.toFixed(2)}%
                                            </span>
                                        </div>
                                    </div>
                                );
                            })}
                        </div>
                    </div>

                    {/* Order Entry Ticket */}
                    <div className="card order-card">
                        <div className="card-header">
                            <h4>Order Ticket: {selectedSymbol}</h4>
                            <div className="order-side-toggle">
                                <button
                                    type="button"
                                    className={`side-btn side-buy ${side === 'BUY' ? 'active' : ''}`}
                                    onClick={() => setSide('BUY')}
                                >
                                    BUY
                                </button>
                                <button
                                    type="button"
                                    className={`side-btn side-sell ${side === 'SELL' ? 'active' : ''}`}
                                    onClick={() => setSide('SELL')}
                                >
                                    SELL
                                </button>
                            </div>
                        </div>

                        {message && (
                            <div className={`alert ${message.type === 'success' ? 'alert-success' : 'alert-error'}`}>
                                {message.text}
                            </div>
                        )}

                        <form onSubmit={handleExecuteOrder} className="order-form p-3">
                            <div className="form-group">
                                <label>Order Type</label>
                                <div className="type-toggle">
                                    <button
                                        type="button"
                                        className={`type-btn ${orderType === 'MARKET' ? 'active' : ''}`}
                                        onClick={() => setOrderType('MARKET')}
                                    >
                                        Market (Instant)
                                    </button>
                                    <button
                                        type="button"
                                        className={`type-btn ${orderType === 'LIMIT' ? 'active' : ''}`}
                                        onClick={() => setOrderType('LIMIT')}
                                    >
                                        Limit Order
                                    </button>
                                </div>
                            </div>

                            {orderType === 'LIMIT' && (
                                <div className="form-group">
                                    <label>Limit Price (USD)</label>
                                    <input
                                        type="number"
                                        step="any"
                                        value={limitPrice}
                                        onChange={(e) => setLimitPrice(e.target.value)}
                                        placeholder="Enter target price"
                                        required
                                    />
                                </div>
                            )}

                            <div className="form-group">
                                <div className="label-with-balance">
                                    <label>Quantity ({selectedSymbol})</label>
                                    <small className="balance-hint">
                                        {side === 'BUY'
                                            ? `Buying Power: $${availableCash.toFixed(2)}`
                                            : `Available: ${availableCryptoQty.toFixed(8)} ${selectedSymbol}`
                                        }
                                    </small>
                                </div>
                                <input
                                    type="number"
                                    step="any"
                                    value={quantity}
                                    onChange={(e) => setQuantity(e.target.value)}
                                    placeholder="0.00000000"
                                    required
                                />
                            </div>

                            {/* Quick Percent Buttons */}
                            <div className="quick-fill-row">
                                <button type="button" className="btn-pct" onClick={() => handleQuickPercent(0.25)}>25%</button>
                                <button type="button" className="btn-pct" onClick={() => handleQuickPercent(0.50)}>50%</button>
                                <button type="button" className="btn-pct" onClick={() => handleQuickPercent(0.75)}>75%</button>
                                <button type="button" className="btn-pct" onClick={() => handleQuickPercent(1.00)}>100%</button>
                            </div>

                            {/* Order Summary Box */}
                            <div className="order-summary-box">
                                <div className="summary-row">
                                    <span>Execution Price:</span>
                                    <strong>${formatMoney(currentTradePrice, 2, 6)}</strong>
                                </div>
                                <div className="summary-row">
                                    <span>Estimated Total:</span>
                                    <strong className="summary-total">${formatMoney(estimatedTotal, 2, 2)} USD</strong>
                                </div>
                                <div className="summary-row text-muted">
                                    <span>Trading Fee (Paper):</span>
                                    <span>$0.00 (Free)</span>
                                </div>
                            </div>

                            <button
                                type="submit"
                                className={`btn btn-block btn-lg ${side === 'BUY' ? 'btn-buy' : 'btn-sell'}`}
                                disabled={loading}
                            >
                                {loading ? 'Processing Order...' : `${side} ${selectedSymbol}`}
                            </button>
                        </form>
                    </div>
                </div>

                {/* ========================================================================= */}
                {/* RIGHT AREA: Interactive Chart with Timeframes & Asset Performance        */}
                {/* ========================================================================= */}
                <div className="trading-right-area">
                    {/* Interactive Crypto Chart */}
                    <CryptoChart
                        symbol={selectedSymbol}
                        crypto={activeCrypto}
                        livePrice={livePrice}
                        backendOffline={backendOffline}
                        onRefreshMarket={onRefreshMarket}
                    />

                    {/* Position Details & Paper Trading Guardrails */}
                    <div className="card position-details-card mt-3">
                        <div className="card-header">
                            <h4>{activeCrypto.name} ({selectedSymbol}) Position & Stats</h4>
                            <span className={`badge ${safeNum(activeCrypto.priceChange24h) >= 0 ? 'badge-success' : 'badge-danger'}`}>
                                24h: {safeNum(activeCrypto.priceChange24h) >= 0 ? '+' : ''}{safeNum(activeCrypto.priceChange24h).toFixed(2)}%
                            </span>
                        </div>

                        <div className="metrics-grid p-3">
                            <div className="metric-card">
                                <div className="metric-label">Your Holding</div>
                                <div className="metric-value">{availableCryptoQty.toFixed(8)} {selectedSymbol}</div>
                                <div className="metric-sub">${formatMoney(availableCryptoQty * livePrice)} USD Market Value</div>
                            </div>
                            <div className="metric-card">
                                <div className="metric-label">Avg Buy Price</div>
                                <div className="metric-value">${formatMoney(currentHolding?.averageCost || 0)}</div>
                                <div className="metric-sub">Cost Basis per coin</div>
                            </div>
                            <div className="metric-card">
                                <div className="metric-label">Unrealized P/L</div>
                                <div className={`metric-value ${(currentHolding?.unrealizedProfitLoss || 0) >= 0 ? 'text-success' : 'text-danger'}`}>
                                    {(currentHolding?.unrealizedProfitLoss || 0) >= 0 ? '+' : ''}${formatMoney(currentHolding?.unrealizedProfitLoss || 0)}
                                </div>
                                <div className="metric-sub">
                                    {(currentHolding?.unrealizedProfitLossPercentage || 0) >= 0 ? '+' : ''}{(currentHolding?.unrealizedProfitLossPercentage || 0).toFixed(2)}% Return
                                </div>
                            </div>
                            <div className="metric-card">
                                <div className="metric-label">Market Price</div>
                                <div className="metric-value">${formatMoney(livePrice, livePrice < 1 ? 4 : 2, livePrice < 1 ? 4 : 2)}</div>
                                <div className="metric-sub">
                                    Refreshes in {countdown}s
                                </div>
                            </div>
                        </div>

                        <div className="trading-rules-note p-3">
                            <h5>Paper Trading Guardrails:</h5>
                            <ul>
                                <li>Market orders execute instantaneously at live CoinGecko prices.</li>
                                <li>Atomic database transactions prevent negative cash balances and overdrafts.</li>
                                <li>Selling computes realized P/L using weighted average cost basis accounting.</li>
                            </ul>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    );
}
