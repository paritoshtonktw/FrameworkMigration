# Controller Migration Instructions: .NET Framework 4.7.2 to .NET 10 (Clean Architecture & Contract Fidelity)

This document provides precise instructions for an AI agent to migrate the legacy ASP.NET Web API 2 controllers to modernized **.NET 10 Web API Controllers**. 

To satisfy the modern design standards, the migrated controllers MUST adhere strictly to **Clean Architecture** and maintain **100% backward API contract compatibility** (matching exact routes, payloads, nested casing, HTTP status codes, and JSON response envelopes).

---

## 1. Clean Architecture Design Rules

The presentation layer (Controllers) must be completely decoupled from the data layer. The AI shall enforce the following architectural rules:

1. **Service-Only Dependencies:** Controllers MUST depend exclusively on Business/Domain Services (e.g. `IAuthService`, `ITradingService`, `IPortfolioService`). Direct injection or instantiation of ADO.NET repositories (`IUserRepository`, etc.) inside controllers is **strictly forbidden**.
2. **Decoupled Business Validation:** Controllers are thin traffic coordinators. All trading limits, wallet check formulas, or financial logic must reside within the business service layers—never in the Controller actions.
3. **Implicit User Context Parsing:** Do not parse or decode JWT tokens inside actions. The security claims principal must be processed implicitly by the native ASP.NET Core Bearer Middleware, with the controller accessing user properties through secure base helpers.
4. **Strong Typing:** Use concrete request classes (e.g., `BuyTradeRequest`) and strongly-typed payload bindings (`[FromBody]`). Avoid untyped model bindings.

---

## 2. API Contract & Routing Mappings

To maintain complete backward compatibility with the existing React SPA frontend, the new routing scheme must match the legacy controllers exactly:

| Endpoint Path | Method | Auth Required | Description | Legacy Controller | Target Controller |
| :--- | :--- | :--- | :--- | :--- | :--- |
| `/api/auth/register` | `POST` | No | User registration | `AuthController` | `AuthController` |
| `/api/auth/login` | `POST` | No | User authentication | `AuthController` | `AuthController` |
| `/api/cryptocurrencies` | `GET` | Yes | List market cryptos | `CryptocurrenciesController` | `CryptocurrenciesController` |
| `/api/portfolio` | `GET` | Yes | Get real-time portfolio | `PortfolioController` | `PortfolioController` |
| `/api/orders/buy` | `POST` | Yes | Place a BUY order | `OrdersController` | `OrdersController` |
| `/api/orders/sell` | `POST` | Yes | Place a SELL order | `OrdersController` | `OrdersController` |
| `/api/trades` | `GET` | Yes | Retrieve trade history | `TradesController` | `TradesController` |
| `/api/transactions` | `GET` | Yes | Retrieve financial ledger | `TransactionsController` | `TransactionsController` |

---

## 3. Step 1: Base Controller Scaffolding

To automate claims extraction and enforce the uniform response envelope structure, the AI shall scaffold a common `BaseApiController` class:

```csharp
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace CryptoTrading.Core.Controllers;

[ApiController]
[Produces("application/json")]
public abstract class BaseApiController : ControllerBase
{
    /// <summary>
    /// Safely extracts the authenticated User ID from the JWT NameIdentifier claim.
    /// </summary>
    protected int CurrentUserId
    {
        get
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(idClaim) || !int.TryParse(idClaim, out var userId))
            {
                throw new UnauthorizedAccessException("User identification claim is missing or invalid.");
            }
            return userId;
        }
    }

    /// <summary>
    /// Formats success payloads matching the standard legacy Uniform Response Envelope.
    /// </summary>
    protected IActionResult EnvelopeOk<T>(T data, string message = "Operation completed successfully.")
    {
        return Ok(new
        {
            success = true,
            message,
            data
        });
    }

    /// <summary>
    /// Formats creation payloads matching the standard legacy Uniform Response Envelope (HTTP 201).
    /// </summary>
    protected IActionResult EnvelopeCreated<T>(string uri, T data, string message = "Resource created successfully.")
    {
        return Created(uri, new
        {
            success = true,
            message,
            data
        });
    }
}
```

---

## 4. Step 2: Migrating Authentication Endpoints (`AuthController`)

The Auth endpoint must handle anonymous registration and login, returning JSON properties identical to the legacy design:

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CryptoTrading.Business.Services;
using CryptoTrading.Models.Requests;

namespace CryptoTrading.Core.Controllers;

[Route("api/auth")]
[AllowAnonymous]
public class AuthController : BaseApiController
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterUserRequest request)
    {
        if (request == null)
            return BadRequest(new { success = false, message = "Request payload cannot be empty.", errorCode = "INVALID_ARGUMENT" });

        var result = await _authService.RegisterAsync(request);
        
        // Match exact legacy 201 response contract (Location header pointing to profile)
        return EnvelopeCreated($"/api/profile/{result.User.UserId}", result, "User registered successfully.");
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (request == null)
            return BadRequest(new { success = false, message = "Credentials cannot be empty.", errorCode = "INVALID_ARGUMENT" });

        var result = await _authService.LoginAsync(request);
        return EnvelopeOk(result, "User logged in successfully.");
    }
}
```

---

## 5. Step 3: Migrating Authenticated Endpoints (`OrdersController`)

Secure endpoints require bearer validation and MUST retrieve the user ID implicitly from JWT claims, mapping the payload to business logic:

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CryptoTrading.Business.Services;
using CryptoTrading.Models.Requests;

namespace CryptoTrading.Core.Controllers;

[Route("api/orders")]
[Authorize]
public class OrdersController : BaseApiController
{
    private readonly ITradingService _tradingService;

    public OrdersController(ITradingService tradingService)
    {
        _tradingService = tradingService;
    }

    [HttpPost("buy")]
    public async Task<IActionResult> Buy([FromBody] BuyTradeRequest request)
    {
        if (request == null)
            return BadRequest(new { success = false, message = "Trade arguments cannot be empty.", errorCode = "INVALID_ARGUMENT" });

        // Retrieve the authenticated User ID implicitly from Claims
        int userId = CurrentUserId;

        // Delegate entire validation and database logic to the trading service
        var tradeResult = await _tradingService.BuyAsync(userId, request);

        return EnvelopeOk(tradeResult, "Buy trade executed successfully.");
    }

    [HttpPost("sell")]
    public async Task<IActionResult> Sell([FromBody] SellTradeRequest request)
    {
        if (request == null)
            return BadRequest(new { success = false, message = "Trade arguments cannot be empty.", errorCode = "INVALID_ARGUMENT" });

        int userId = CurrentUserId;
        var tradeResult = await _tradingService.SellAsync(userId, request);

        return EnvelopeOk(tradeResult, "Sell trade executed successfully.");
    }
}
```

---

## 6. Step 4: Configure JSON Serialization in `Program.cs`

To prevent payload mapping breaks in the React client, JSON output MUST preserve casing structures identical to legacy setups. The AI shall register modern controllers in `Program.cs` configured with camelCase properties:

```csharp
// In Program.cs (under builder.Services):
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Enforce camelCasing for dictionary keys, objects, and properties
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DictionaryKeyPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

// Under routing configurations in Program.cs:
app.MapControllers(); // Replaces Minimal API mappings
```

---

## 7. AI Controller Verification Checklist

Before certifying the Controller modernization, the AI shall confirm:

1. **Compilation Check:** Run `dotnet build` from the CLI. Confirm 0 compiler issues.
2. **Contract Fidelity Verification:** Ensure there are no modifications to payload model shapes, response keys, or nested parameters.
3. **No Repository Violations:** Search Controller files to ensure NO references to database context, connection builders, or `IDbConnectionFactory` are directly imported or injected.
4. **Exception Integrity:** Verify that any custom exception thrown by the services (e.g. `ValidationException`) maps to uniform failure envelopes `{ "success": false, "message": "...", "errorCode": "..." }` through global middleware filters.
