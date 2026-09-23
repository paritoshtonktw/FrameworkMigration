# Architecture Documentation — Legacy Cryptocurrency Trading Platform (AS-IS)

## 1. System Philosophy & Monolithic Architecture

The cryptocurrency paper-trading platform is deliberately architected as an **AS-IS legacy enterprise monolith**. The application adheres strictly to classical enterprise .NET patterns common in established financial environments:

1. **Layered Separation of Concerns:** Clear boundaries between presentation, HTTP/API handling, business domain validation, data access, and database logic.
2. **Database-Centric Financial Processing:** All critical financial operations—fiat balance deductions, cryptocurrency wallet increments, trade executions, and transaction ledger logging—reside directly in **SQL Server Stored Procedures**. Business and presentation layers orchestrate requests but delegate atomic financial mutability to the database.
3. **Pure ADO.NET Data Access:** No ORM frameworks (Entity Framework, Dapper, NHibernate) are utilized. All interactions are executed via explicit `SqlConnection`, `SqlCommand`, parameterized `@` variables, and streaming `SqlDataReader`.
4. **Pessimistic Concurrency & Atomicity:** Operations that mutate financial balances execute under `BEGIN TRANSACTION` with SQL Server table hints `WITH (UPDLOCK, HOLDLOCK)` to eliminate race conditions, double spending, and phantom reads.

```text
+-------------------------------------------------------------------------+
|                Crypto Trader React (Separate React Project)             |
|             (SPA: Auth, Dashboard, Trading, Portfolio, Ledger)         |
+-------------------------------------------------------------------------+
                                     |
                                     | HTTP REST (JSON, JWT Bearer, CORS)
                                     v
+-------------------------------------------------------------------------+
|           Crypto Trading — ASP.NET Web API 2 (net472 / IIS Express)     |
|                      (Pure Headless Backend API)                        |
|  - Routing: Attribute Routing [RoutePrefix("api/...")]                  |
|  - Filters: JwtAuthorizeFilter, ApiExceptionFilterAttribute             |
|  - Dependency Injection: Pure DI Composition via DependencyConfig       |
+-------------------------------------------------------------------------+
                                     |
                                     v
+-------------------------------------------------------------------------+
|                        Business Logic Services                          |
|  - AuthService: PBKDF2 Password verification & JWT issuance             |
|  - TradingService: Price validation, order processing & trade execution |
|  - PortfolioService: Real-time portfolio valuation & P/L calculation   |
|  - FinancialService: Deposit & withdrawal coordination                  |
|  - CoinGeckoMarketService: 45s in-memory TTL caching + SQL fallback     |
+-------------------------------------------------------------------------+
                                     |
                                     v
+-------------------------------------------------------------------------+
|                    Data Access Layer (Pure ADO.NET)                     |
|  - SqlConnectionFactory: Connection lifetime management                 |
|  - Repositories: UserRepository, AccountRepository, CryptoRepository,  |
|                  WalletRepository, OrderRepository, TradeRepository,    |
|                  TransactionRepository, DepositRepository              |
+-------------------------------------------------------------------------+
                                     |
                                     v
+-------------------------------------------------------------------------+
|                 Microsoft SQL Server 2022 / LocalDB                     |
|  - Tables: Strict constraints (Balance >= 0, Quantity > 0, Foreign Keys) |
|  - 28 Stored Procedures with explicit BEGIN TRANSACTION / ROLLBACK     |
|  - Concurrency control: WITH (UPDLOCK, HOLDLOCK)                       |
|  - Views: vw_UserPortfolioSummary, vw_UserActiveHoldings               |
+-------------------------------------------------------------------------+
```

---

## 2. Layer Detailed Responsibilities

### 2.1 Presentation Layer: `Crypto Trader React` (Separate React 18 Project)
- Maintained as an independent project directory (`Crypto Trader React/`) completely decoupled from the .NET backend.
- Communicates exclusively over JSON REST APIs with CORS support enabled on the backend.
- Run via standard npm development workflows (`npm start`).
- Maintains user session via `localStorage` JWT token storage.
- Implements responsive desktop/tablet UI across all 9 required screens:
  - Auth: Login (`35.1`), Registration (`35.2`)
  - Trading & Market: Dashboard (`35.3`), Order Entry (`35.4`)
  - Financial Management: Portfolio (`35.5`), Orders (`35.6`), Ledger (`35.7`), Funds (`35.8`), Profile (`35.9`)
- Real-time polling timer refreshes live market quotes and ticker data periodically.

### 2.2 API Layer: ASP.NET Web API 2 (`CryptoTrading.Web`)
- Built on .NET Framework 4.7.2.
- Uses `System.Web.Http` attribute routing.
- **Cross-Cutting Filters:**
  - `JwtAuthorizeAttribute`: Validates HMAC-SHA256 signature, expiry, and extracts `ClaimTypes.NameIdentifier` into `Thread.CurrentPrincipal` / `HttpContext.Current.User`.
  - `ApiExceptionFilterAttribute`: Catches unhandled exceptions, logs errors via log4net, and formats uniform error envelopes (`{ success: false, message: "...", errorCode: "..." }`).
- **Dependency Injection:** Configured manually without heavy 3rd-party IoC containers using `DependencyConfig` and custom `IDependencyResolver`.

### 2.3 Business Logic Layer (`CryptoTrading.Business`)
- Encapsulates domain logic and business rules:
  - Validates minimum/maximum order quantities, deposit limits, and withdrawal ceilings.
  - Interacts with `IMarketDataService` to acquire current cryptocurrency pricing.
  - Combines stored procedure outputs with live prices to calculate real-time portfolio metrics (holdings value, total portfolio value, unrealized P/L).
  - Handles business exceptions (`ValidationException`, `InvalidOperationException`, `UnauthorizedAccessException`).

### 2.4 Infrastructure Layer (`CryptoTrading.Infrastructure`)
- **Authentication Services:**
  - `Pbkdf2PasswordHasher`: PBKDF2 with SHA-256 HMAC, 128-bit cryptographically secure salt, and 10,000 iterations.
  - `JwtTokenService`: Signs JWT tokens with 256-bit symmetric security keys and 24-hour expiration.
- **External Market Data:**
  - `CoinGeckoMarketService`: Communicates with CoinGecko Public V3 API (`https://api.coingecko.com/api/v3/`).
  - Implements an in-memory cache with 45-second sliding expiration to respect CoinGecko free-tier rate limits (10-30 req/min).
  - Resilient fallback: If CoinGecko is unavailable or rate-limited (HTTP 429), automatically retrieves the latest stored prices from SQL Server (`usp_GetCryptocurrencies` / `usp_GetLatestCryptoPrice`).
- **Logging:**
  - `Log4NetLoggerService`: File and console logging with structured timestamps and severity levels.

### 2.5 Data Access Layer (`CryptoTrading.Data`)
- Pure ADO.NET using `System.Data.SqlClient`.
- Every query is strictly parameterized via `SqlParameter` preventing SQL injection vulnerabilities.
- Handles DB connection opening, execution, reader iteration, and disposal cleanly via `using` blocks.
- Delegates business logic to Stored Procedures via `CommandType.StoredProcedure`.

### 2.6 Database Layer (`database/`)
- Relational schema optimized for financial consistency and historical auditability.
- Constraints enforce non-negative balances (`Balance >= 0`), non-negative wallet quantities (`Quantity >= 0`), and positive transaction values (`Amount > 0`).
- Stored procedures handle all multi-table mutations within explicit transactions (`BEGIN TRANSACTION`, `COMMIT TRANSACTION`, `ROLLBACK TRANSACTION`).

---

## 3. Financial Calculation Models

### 3.1 Weighted Average Cost Basis (Average Buy Price)
When a user buys additional units of a cryptocurrency, the new weighted average buy price is calculated in SQL Server:

$$\text{New Avg Price} = \frac{(\text{Current Quantity} \times \text{Current Avg Price}) + (\text{Buy Quantity} \times \text{Execution Price})}{\text{Current Quantity} + \text{Buy Quantity}}$$

### 3.2 Realized Profit / Loss (On SELL)
Upon execution of a sell order, realized profit or loss is locked into the trade record:

$$\text{Realized P/L} = (\text{Execution Price} - \text{Average Buy Price}) \times \text{Sell Quantity}$$

### 3.3 Unrealized Profit / Loss (Mark-to-Market)
Calculated dynamically against the current live market price:

$$\text{Unrealized P/L} = (\text{Current Live Price} - \text{Average Buy Price}) \times \text{Holding Quantity}$$

### 3.4 Total Portfolio Value
$$\text{Total Portfolio Value} = \text{Cash Balance} + \sum (\text{Holding Quantity}_i \times \text{Current Live Price}_i)$$

---

## 4. Concurrency & Transaction Isolation

To prevent race conditions, such as:
- Placing two concurrent buy orders that exceed available cash balance.
- Placing two concurrent sell orders that exceed available cryptocurrency wallet quantity.
- Withdrawing funds while a concurrent trade is executing.

All balance-mutating stored procedures (`usp_ExecuteBuyTrade`, `usp_ExecuteSellTrade`, `usp_UpdateAccountBalance`, `usp_CreateWithdrawal`) employ pessimistic row locks:

```sql
SELECT @CurrentBalance = Balance
FROM dbo.Accounts WITH (UPDLOCK, HOLDLOCK)
WHERE UserId = @UserId;
```

- `UPDLOCK`: Acquires an update lock preventing other transactions from acquiring an update or exclusive lock on the row.
- `HOLDLOCK`: Holds the lock until the enclosing transaction commits or rolls back, enforcing serializable consistency for that specific account or wallet row.

If balance or quantity is insufficient, an error is raised via `RAISERROR`, triggering the `CATCH` block to execute `ROLLBACK TRANSACTION`, guaranteeing zero partial mutations.

---

## 5. Security Architecture

1. **Authentication:** Stateless Bearer JWT tokens signed with HMAC-SHA256. Tokens include standard claims (`nameid`, `unique_name`, `email`, `given_name`, `family_name`, `nbf`, `exp`, `iss`, `aud`).
2. **Authorization & Data Isolation:**
   - Web API endpoints extract `UserId` strictly from the validated `ClaimsPrincipal`.
   - Client requests cannot pass `?userId=123` to inspect or manipulate other users' data.
   - Every stored procedure filters records strictly by `@UserId`.
3. **Password Security:**
   - Passwords are never stored in plaintext.
   - Hashed using PBKDF2-SHA256 with a unique 16-byte random salt per user and 10,000 iterations.
   - Formatted as: `10000:{base64Salt}:{base64Hash}`.
4. **Input Validation:**
   - Client-side validation for instant feedback.
   - Server-side DataAnnotation validation (`[Required]`, `[Range]`, `[StringLength]`, `[EmailAddress]`).
   - Database check constraints (`CK_Accounts_Balance_NonNegative`, `CK_Wallets_Quantity_NonNegative`, etc.) as the final line of defense.

