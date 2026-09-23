# Crypto Trader React — Frontend Application

A modular React 18 Single-Page Application (SPA) designed to interface with the uplifted ASP.NET Core backend of the **CryptoTrading** platform.

This project is completely decoupled from the .NET backend solution and resides in its own standalone directory.

---

## 1. Project Architecture & Structure

```text
Crypto Trader React/
├── package.json              # Standard npm dependencies & scripts
├── README.md                 # Frontend documentation
├── index.html                # Standalone zero-install runnable (runs immediately in any browser)
├── public/
│   └── index.html            # Standard React HTML template
└── src/
    ├── api/
    │   └── client.js         # Centralized REST API client (http://localhost:5152/api)
    ├── components/
    │   ├── Navbar.jsx        # Sticky navigation with live cash & portfolio balances
    │   ├── Login.jsx         # Sign-in with quick 1-click demo logins (35.1)
    │   ├── Register.jsx      # New account registration with $10,000 cash grant (35.2)
    │   ├── Dashboard.jsx     # Live CoinGecko tickers & portfolio overview (35.3)
    │   ├── Trading.jsx       # Order entry for BUY/SELL Market & Limit orders (35.4)
    │   ├── Portfolio.jsx     # Holdings table with cost basis & unrealized P/L (35.5)
    │   ├── Orders.jsx        # Active & past order blotter with cancellation (35.6)
    │   ├── Trades.jsx        # Executed trade ledger with realized P/L
    │   ├── Ledger.jsx        # Financial transaction audit trail (35.7)
    │   ├── Funds.jsx         # Deposit & withdrawal simulator (35.8)
    │   └── Profile.jsx       # User profile details & editing (35.9)
    ├── App.jsx               # Root application router and polling coordinator
    ├── index.jsx             # React DOM entry point
    └── styles.css            # Dark financial terminal UI styling
```

---

## 2. How to Run

### Option A: Immediate Zero-Install Launch (No Node.js Required)

You do not need Node.js or npm installed to run the application:

1. Ensure the ASP.NET Core backend is running on `http://localhost:5152`.
2. Double-click or open `Crypto Trader React/index.html` in any web browser (Google Chrome, Microsoft Edge, Mozilla Firefox).
3. The application loads React 18 and Babel directly, connects to the local Web API, and is ready for trading immediately!

### Option B: Using Node.js & npm (Development Server)

If Node.js 18+ is installed on your machine:

1. Open a terminal in this directory:
   ```bash
   cd "Crypto Trader React"
   ```
2. Install dependencies:
   ```bash
   npm install
   ```
3. Launch the development server:
   ```bash
   npm start
   ```
4. Access the application at `http://localhost:3000`.

---

## 3. Backend Configuration

The API client in `src/api/client.js` is configured to target the ASP.NET Core backend:

```javascript
const API_BASE_URL = 'http://localhost:5152/api';
```

If hosting the backend on a different port or domain, update `API_BASE_URL` accordingly. The backend has Cross-Origin Resource Sharing (CORS) enabled (`*`) to allow cross-origin requests.

---

## 4. Pre-Seeded Demo Accounts

Click the quick-login buttons on the sign-in screen or use these credentials:

| Username | Password | Cash Balance | Starting Portfolio |
|---|---|---|---|
| `demo_trader` | `Password123!` | $10,000.00 | Fresh account ready for new trades |
| `trader1` | `Password123!` | $14,000.00 | Existing BTC & ETH holdings |
| `trader2` | `Password123!` | $5,000.00 | Existing SOL & ADA holdings |

