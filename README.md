# CryptoTrading — Legacy Cryptocurrency Paper-Trading Platform

An enterprise-grade cryptocurrency paper-trading platform implemented as an **AS-IS legacy monolith** in accordance with `Legacy_Crypto_Trading_Platform_AS_IS_SRS.md`.

The system simulates realistic cryptocurrency trading, portfolio valuation, cash deposits/withdrawals, ledger audit trails, and live market data using CoinGecko. All critical financial operations, balance modifications, and trade executions are executed atomically within **Microsoft SQL Server Stored Procedures** using pessimistic concurrency locking (`UPDLOCK, HOLDLOCK`).

---

## 1. Prerequisites & System Requirements

- **Operating System:** Windows 10 / Windows 11 / Windows Server 2016+
- **.NET Framework:** .NET Framework 4.7.2 Developer Pack or Runtime
- **Build Tools:** Visual Studio 2022 / 2025 (with `.NET desktop development` and `ASP.NET and web development` workloads) or standalone MSBuild 17/18
- **Database (choose one):**
  - **Option A (LocalDB):** Microsoft SQL Server LocalDB (included with Visual Studio)
  - **Option B (Docker):** Docker Desktop (with Linux/Windows containers) running SQL Server 2022
- **Command Shell:** PowerShell 5.1+ or PowerShell Core 7+
- **Frontend Runtime:** Modern web browser (Chrome, Edge, Firefox). The React 18 frontend is located in its own separate project directory: `Crypto Trader React/`.

---

## 2. Architecture Overview

The system strictly follows the decoupled layered legacy pattern:

```text
+-------------------------------------------------------------+
|              Crypto Trader React (Separate Project)         |
|             React 18 SPA (Dashboard, Trading, Portfolio)    |
+-------------------------------------------------------------+
                              | REST JSON (HTTP / Bearer JWT / CORS)
                              v
+-------------------------------------------------------------+
|         Crypto Trading — ASP.NET Web API 2 (net472)         |
|              (Pure Headless Backend API Service)            |
|   - Attribute Routing & Global ApiExceptionFilterAttribute   |
|   - Custom JwtAuthorizeFilter & ClaimsPrincipal Context     |
|   - Manual Pure DI Composition (DependencyConfig)          |
+-------------------------------------------------------------+
                              |
                              v
+-------------------------------------------------------------+
|                 Business Logic Layer                        |
|   (AuthService, TradingService, FinancialService, etc.)    |
|   - CoinGeckoMarketService (45s TTL cache & SQL fallback)   |
+-------------------------------------------------------------+
                              |
                              v
+-------------------------------------------------------------+
|               Data Access Layer (Pure ADO.NET)              |
|   - SqlConnectionFactory & Strongly-typed Repositories      |
|   - No ORM; 100% Parameterized Stored Procedure Execution   |
+-------------------------------------------------------------+
                              |
                              v
+-------------------------------------------------------------+
|                 Microsoft SQL Server                        |
|   - 10 Tables, Strict Check Constraints, Foreign Keys       |
|   - 28 Stored Procedures (UPDLOCK, HOLDLOCK, Atomic XACT)   |
|   - Views: vw_UserPortfolioSummary, vw_UserActiveHoldings   |
+-------------------------------------------------------------+
```

---

## 3. Database Setup & Initialization

The project uses native Microsoft SQL Server scripts for database migrations and initialization (no external migration frameworks like gomigrate).

### Option A: Using LocalDB (Recommended for Windows Development)

1. Open PowerShell and navigate to the project directory:
   ```powershell
   cd "C:\Users\Paritosh Tonk\source\repos\Migration Project"
   ```

2. Run the automated database initialization script:
   ```powershell
   powershell -ExecutionPolicy Bypass -File database/scripts/init-db.ps1 -Server "(localdb)\CryptoTradingDB" -Database "CryptoTradingDB"
   ```

   This script will:
   - Ensure the LocalDB instance `CryptoTradingDB` is created and started.
   - Execute `database/scripts/RunAll.sql` which applies the schema (`01_Tables.sql`), constraints (`02_Constraints.sql`), indexes (`03_Indexes.sql`), views, all 28 stored procedures, migration tracking in `__SchemaMigrations`, and seeds initial demo data.

### Option B: Using Docker SQL Server 2022

1. Launch SQL Server via Docker Compose:
   ```bash
   cd docker
   docker-compose up -d
   ```

2. Wait ~15 seconds for SQL Server to initialize, then run the initialization script based on your operating system:

   - **On macOS / Linux (using native Docker command):**
     Run the following command from the workspace root to execute the master SQL initialization script inside the container:
     ```bash
     docker exec -i cryptotrading-sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "CryptoTrading!2026Secure" -C -i /docker-entrypoint-initdb.d/scripts/RunAll.sql
     ```

   - **On Windows (using PowerShell):**
     ```powershell
     powershell -ExecutionPolicy Bypass -File database/scripts/init-db.ps1 -Server "localhost,1433" -Database "CryptoTradingDB" -User "sa" -Password "CryptoTrading!2026Secure"
     ```

---

## 4. Configuration

The backend configuration is managed via `Crypto Trading/Crypto Trading/Web.config`:

```xml
<connectionStrings>
  <!-- LocalDB Connection -->
  <add name="CryptoTradingDB" 
       connectionString="Server=(localdb)\CryptoTradingDB;Database=CryptoTradingDB;Integrated Security=True;TrustServerCertificate=True;" 
       providerName="System.Data.SqlClient" />
  
  <!-- Docker SQL Server Connection (if running in Docker) -->
  <!--
  <add name="CryptoTradingDB" 
       connectionString="Server=localhost,1433;Database=CryptoTradingDB;User Id=sa;Password=CryptoTrading!2026Secure;TrustServerCertificate=True;" 
       providerName="System.Data.SqlClient" />
  -->
</connectionStrings>

<appSettings>
  <!-- Security & JWT Configuration -->
  <add key="JwtSecret" value="SuperSecretKeyForCryptoTradingPlatform2026!ChangeInProd" />
  <add key="JwtIssuer" value="CryptoTradingPlatform" />
  <add key="JwtAudience" value="CryptoTradingClient" />
  
  <!-- CoinGecko API & Cache TTL -->
  <add key="CoinGeckoBaseUrl" value="https://api.coingecko.com/api/v3/" />
  <add key="CacheDurationSeconds" value="45" />
</appSettings>
```

---

## 5. Building & Running the Backend

The platform supports both the modern uplifted .NET Core backend and the legacy .NET Framework backend. The React frontend is pre-configured to connect to the modern backend by default.

### Modern Uplifted .NET Core Backend (Recommended)

The modernized backend is located in the `CryptoTrading.Core` directory and is built using ASP.NET Core (.NET 10.0).

#### Running via .NET CLI:
1. Open a terminal and navigate to the project directory:
   ```bash
   cd CryptoTrading.Core
   ```
2. Run the application:
   ```bash
   dotnet run --project src/CryptoTrading.Core
   ```
   Or from the workspace root:
   ```bash
   dotnet run --project CryptoTrading.Core/src/CryptoTrading.Core/CryptoTrading.Core.csproj
   ```

#### Once running:
- **Default Start Page (Swagger UI):** `http://localhost:5152/swagger` (or `https://localhost:7164/swagger`)
- **Backend Web API URL:** `http://localhost:5152/api/`
- **OpenAPI Schema JSON:** `http://localhost:5152/openapi/v1.json`
- **CORS Support:** Pre-configured to support React development server origins (such as `http://localhost:3000`).

---

### Legacy .NET Framework Backend (Legacy)

#### Building with MSBuild or Visual Studio

1. Open `Crypto Trading/Crypto Trading.sln` (or `Crypto Trading.slnx`) in Visual Studio, or build from PowerShell:
   ```powershell
   & "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" "Crypto Trading\Crypto Trading.sln" /p:Configuration=Debug /v:m
   ```

#### Running via IIS Express

Run IIS Express from PowerShell:
```powershell
& "C:\Program Files\IIS Express\iisexpress.exe" /path:"C:\Users\Paritosh Tonk\source\repos\Migration Project\Crypto Trading\Crypto Trading" /port:44341
```

Once running:
- **Default Start Page (Swagger UI):** `http://localhost:44341/swagger` (or navigating to `http://localhost:44341/` automatically redirects to Swagger)
- **Backend Web API URL:** `http://localhost:44341/api/`
- **Swagger Docs JSON:** `http://localhost:44341/swagger/docs/v1`
- **CORS Support:** Enabled for all incoming frontend origins.

---

### Running the React Frontend (`Crypto Trader React`)

The React frontend is configured to target the modern, uplifted .NET Core backend running at `http://localhost:5152/api` by default.

Run via Node / npm (Development Server):
```bash
cd "Crypto Trader React"
npm install
npm start
```

---

## 6. Running Automated Tests

The solution includes comprehensive unit and integration test suites covering authentication, trade execution, financial atomicity, and stored procedure interaction against LocalDB:

### Modern Uplifted .NET Core Backend Tests (Recommended)

Run tests for the modernized ASP.NET Core project:
```bash
dotnet test "CryptoTrading.Core/tests/CryptoTrading.Core.Tests/CryptoTrading.Core.Tests.csproj" -v normal
```

Expected result:
```text
Passed! - Failed: 0, Passed: 25, Skipped: 0, Total: 25
```

### Legacy .NET Framework Backend Tests

Run tests for the legacy .NET Framework project:
```powershell
dotnet test "Crypto Trading/tests/CryptoTrading.Tests/CryptoTrading.Tests.csproj" -v normal
```

Expected result:
```text
Passed! - Failed: 0, Passed: 29, Skipped: 0, Total: 29
```

---

## 7. Demo Credentials

The database is pre-seeded with 3 demo accounts. All accounts share the same password:

| Username | Email | Password | Initial Balance | Description |
|---|---|---|---|---|
| `demo_trader` | `bob@cryptotrading.local` | `Password123!` | $10,000.00 USD | Clean demo account ready for immediate trading |
| `trader1` | `alice@cryptotrading.local` | `Password123!` | $14,000.00 USD | Active trader with pre-existing BTC & ETH holdings |
| `trader2` | `carol@cryptotrading.local` | `Password123!` | $5,000.00 USD | Trader with SOL & ADA holdings |

---

## 8. Frontend Application Features

The separate single-page application in `Crypto Trader React/` covers all 9 screens required by SRS Section 35:

1. **Login (35.1):** Secure JWT authentication with pre-filled demo account buttons.
2. **Registration (35.2):** New user onboarding with instant $10,000 demo paper cash balance.
3. **Dashboard (35.3):** Live market ticker cards (BTC, ETH, SOL, ADA, XRP), net worth summary, unrealized/realized P/L badges, quick action links.
4. **Trading Screen (35.4):** Real-time order execution with live price quotes, percentage quick-fill buttons (25%, 50%, 75%, 100%), estimated fee/proceeds calculation, and instant balance updates.
5. **Portfolio Screen (35.5):** Holdings table with symbol, units held, average buy price, current market price, market value, unrealized P/L, and allocation percentage.
6. **Orders Screen (35.6):** Tabbed view for active and past orders, with status badges (`PENDING`, `FILLED`, `CANCELLED`, `REJECTED`) and cancel order actions.
7. **Trades Screen:** Executed trade log displaying trade prices, quantities, and realized P/L per transaction.
8. **Ledger / Transactions Screen (35.7):** Audit trail of every financial event (`DEPOSIT`, `WITHDRAWAL`, `BUY`, `SELL`) with before-and-after cash balances.
9. **Funds Screen (35.8):** Deposit and withdrawal simulator with instant atomic SQL updates.
10. **Profile Screen (35.9):** Account details, registration timestamp, and editable contact info.

---

## 9. API Documentation & Testing

The REST API exposes the following primary endpoints:

- `POST /api/auth/register` — Register a new trader
- `POST /api/auth/login` — Authenticate and retrieve JWT Bearer token
- `GET /api/cryptocurrencies` — List supported coins with live CoinGecko prices
- `GET /api/portfolio` — Current user portfolio valuation and crypto holdings
- `POST /api/orders` — Place atomic BUY / SELL market and limit orders
- `GET /api/orders` — View user order history
- `DELETE /api/orders/{id}` — Cancel pending limit order
- `GET /api/trades` — View user executed trades and realized P/L
- `GET /api/transactions` — View financial ledger audit trail
- `POST /api/deposits` — Simulated cash deposit
- `POST /api/withdrawals` — Simulated cash withdrawal
- `GET /api/profile` — View user profile and account details
- `POST /api/pubsub/order-placed` — Google Cloud Pub/Sub native Push Subscription endpoint (Base64 unwrapping, FIFO execution)
- `POST /api/pubsub/order-executed` — Post-execution audit and notification Push Subscription endpoint

For complete endpoint specifications, request/response schemas, and example payloads, refer to [`docs/api.md`](docs/api.md).

---

## 10. Google Cloud Pub/Sub & GCP Serverless Readiness

The platform includes a Google Cloud Pub/Sub messaging architecture optimized for **GCP Cloud Run**:
- **Topics:** `crypto-orders-incoming`, `crypto-orders-executed`, `crypto-market-ticks`.
- **Strict FIFO per Asset:** Uses Pub/Sub `orderingKey = Symbol` (e.g. `BTC`, `ETH`) for sequential order execution.
- **Serverless Push Subscriptions:** Cloud Run receives order matching events via HTTPS POST to `POST /api/pubsub/order-placed`, auto-acknowledging with HTTP 200 OK.
- **Sub-Millisecond In-Memory Cache:** `MarketTickHotCacheSubscriber` maintains live market prices in a thread-safe hot cache.
- **Local Emulation:** Run `docker-compose up -d pubsub-emulator` from `docker/` for local development against port `8085`.
- **Graceful Fallback:** If Pub/Sub is disabled or unreachable, the system automatically falls back to an in-process event hub without throwing exceptions.

---

## 11. Additional Documentation

- [`docs/architecture.md`](docs/architecture.md) — Detailed AS-IS layered monolith design, security, and concurrency models.
- [`docs/legacy-code-analysis.md`](docs/legacy-code-analysis.md) — In-depth legacy anti-patterns, inconsistent coding styles, and architectural debt catalog.
- [`docs/database.md`](docs/database.md) — Schema reference, data types, precision definitions, indexes, and migration strategy.
- [`docs/stored-procedures.md`](docs/stored-procedures.md) — Comprehensive catalog of all 28 stored procedures, locking semantics, and parameter contracts.
- [`docs/api.md`](docs/api.md) — Complete REST API contract and status code catalog.

