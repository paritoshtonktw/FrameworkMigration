# Controller Migration Instructions: .NET Framework 4.7.2 to .NET 10 (Surgical Vertical Slice & Clean Architecture Transformation)

This document provides precise instructions for an AI agent to migrate a **single, specific target controller** and its entire downstream dependency graph from legacy ASP.NET Web API 2 to a modernized **.NET 10 Web API Controller** project.

Rather than migrating the entire system at once, the migration MUST follow a **Vertical Slice** pattern. Only the target controller specified in the input, along with its dependent files, shall be migrated and modernized.

---

## 1. Clean Architecture Transformation Mandate

The modernized target controller MUST adhere strictly to **Clean Architecture**. If the legacy codebase is not in Clean Architecture (e.g., if the controller accesses repositories or the database directly), the AI **MUST** refactor and reorganize the code during migration:

1. **Service-Only Dependencies:** Controllers MUST depend exclusively on Business/Domain Services (e.g., `IAuthService`, `ITradingService`, `IFinancialService`). **Direct injection, instantiation, or reference of data repositories (e.g., `IUserRepository`, `IDepositRepository`) or database factories inside controllers is strictly forbidden.**
2. **Decoupled Business Validation:** Controllers are thin traffic coordinators. All trading limits, wallet check formulas, financial validations, or workflow logic must reside within the business service layers—never in the controller actions.
3. **Database Separation:** All database access must be completely isolated behind repositories and services. The controller must have zero knowledge of database schemas, SQL queries, or repository boundaries.
4. **Implicit User Context Parsing:** Do not parse or decode JWT tokens manually inside actions. The security claims principal must be processed implicitly by the native ASP.NET Core Bearer Middleware, with the controller accessing user properties through secure base helpers.
5. **Strong Typing:** Use concrete request classes (e.g., `BuyTradeRequest`) and strongly-typed payload bindings (`[FromBody]`, `[FromQuery]`). Avoid untyped model bindings.

---

## 2. Surgical Vertical Slice Migration Protocol

When given a target legacy controller to migrate, the AI shall execute the following five-step protocol:

```text
+-----------------------------------------------------------------+
|                  Step 1: Dependency Analysis                    |
| Trace target controller -> Services -> Repositories -> Models   |
+-----------------------------------------------------------------+
                                |
                                v
+-----------------------------------------------------------------+
|               Step 2: Data & Model Modernization                |
| Migrate/scaffold dependent DTOs, Entities & Dapper Repositories|
+-----------------------------------------------------------------+
                                |
                                v
+-----------------------------------------------------------------+
|            Step 3: Business Service Modernization               |
| Create/extend Services; extract any direct DB logic to Service  |
+-----------------------------------------------------------------+
                                |
                                v
+-----------------------------------------------------------------+
|            Step 4: Scaffold Modernized Controller               |
| Inherit BaseApiController, map exact legacy routes & contract   |
+-----------------------------------------------------------------+
                                |
                                v
+-----------------------------------------------------------------+
|                Step 5: Dependency Injection & Build             |
| Register only the migrated slice and run compilation checks      |
+-----------------------------------------------------------------+
```

### Step 1: Dependency Analysis
1. Read the legacy controller file to identify all actions, routes, HTTP methods, and parameters.
2. Track all downstream dependencies:
   - Identify which legacy services and interfaces are consumed.
   - Identify which repositories and database-access structures are called (either by the services or directly by the controller).
   - Identify all request models, response models, DTOs, and entities involved.
3. This set of identified files forms the **Vertical Slice** that must be migrated together.

### Step 2: Data & Model Modernization
1. Create modernized, strongly-typed C# classes for all dependent models, DTOs, and request objects. Place them in the corresponding `Models/` directories (`DTOs/`, `Entities/`, `Requests/`).
2. **Strict Validation & Nullability Alignment:** Carefully review the frontend/legacy client JSON payloads to align model validation rules and optional fields. Ensure fields passed as `null` or omitted (such as `price` on `MARKET` orders) are typed as nullable in modern C# DTOs (e.g., `decimal? Price`) to prevent model state binding failures (HTTP 400 Bad Request) caused by framework constraints.
3. Implement or update the dependent repositories to use .NET 10 standards with **Dapper** as described in `MIGRATION_INSTRUCTIONS.md`. Ensure all repository methods are fully asynchronous and wrap connections in scoped `using` blocks.

### Step 3: Business Service Modernization & Refactoring
1. **Direct DB Refactoring:** If the legacy controller has direct database queries or repository calls, you must:
   - Introduce a new Business Service interface (e.g., `IFinancialService`) and its implementation (`FinancialService`).
   - Move the database/repository calls from the controller action into the service implementation.
   - Put all business validation and rules inside this service.
2. If the legacy controller already depends on a service, modernize that service to use fully async patterns, proper logging, and transient dependency injection.

### Step 4: Scaffold Modernized Controller
1. The target controller must inherit from the uniform `BaseApiController` (defined in Section 3) to enforce standard claims extraction and response envelopes.
2. **100% Contract & Validation Fidelity:** The target controller endpoints MUST match the legacy controller routes, HTTP verbs, query parameters, and response structures exactly. Do not alter, tighten, or omit validations unless explicitly requested. Pay close attention to keeping exact route patterns, query parameter names, and body payloads.
3. **Serialization Casing:** Ensure JSON outputs match the camelCase requirement of the React SPA frontend.

### Step 5: Dependency Injection & Verification
1. Update `DependencyInjectionExtensions.cs` in the modern project to register only the newly migrated vertical slice dependencies (Repositories, Services).
2. Register the modernized Controller in the application's routing framework.
3. Compile and verify that the vertical slice compiles cleanly.

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

## 4. Architectural Transformation Example: Refactoring Legacy Direct-Access to Clean Architecture

Consider a legacy `DepositsController` that violates Clean Architecture by directly calling repositories and containing processing logic in its actions:

### Legacy Violation (Direct Repository Access & Processing in Controller)
```csharp
// LEGACY VIOLATION: Directly depends on repositories and does processing in controller
public class DepositsController : BaseApiController
{
    private readonly IDepositRepository _depositRepository;
    private readonly IAccountRepository _accountRepository;

    public DepositsController(IDepositRepository depositRepository, IAccountRepository accountRepository)
    {
        _depositRepository = depositRepository;
        _accountRepository = accountRepository;
    }

    [HttpPost]
    [Route("")]
    public async Task<IHttpActionResult> Deposit([FromBody] DepositRequest request)
    {
        // Business logic inside controller action (Violation!)
        if (request.Amount <= 0)
            return BadRequest("Amount must be greater than zero.");

        var account = await _accountRepository.GetAccountByUserIdAsync(CurrentUserId);
        if (account == null)
            return NotFound();

        var result = await _depositRepository.ProcessDepositAsync(CurrentUserId, request.Amount, request.Currency);
        return Ok(result);
    }
}
```

### Modernized Clean Architecture Solution
The modernized code completely decouples the controller, introducing `IFinancialService` to orchestrate business validation and data repository calls:

#### 1. Modernized Business Service Layer
```csharp
namespace CryptoTrading.Business.Services;

public interface IFinancialService
{
    Task<DepositDto> ProcessDepositAsync(int userId, DepositRequest request);
}

public class FinancialService : IFinancialService
{
    private readonly IDepositRepository _depositRepository;
    private readonly IAccountRepository _accountRepository;

    public FinancialService(IDepositRepository depositRepository, IAccountRepository accountRepository)
    {
        _depositRepository = depositRepository;
        _accountRepository = accountRepository;
    }

    public async Task<DepositDto> ProcessDepositAsync(int userId, DepositRequest request)
    {
        if (request.Amount <= 0)
            throw new ArgumentException("Amount must be greater than zero.");

        var account = await _accountRepository.GetAccountByUserIdAsync(userId);
        if (account == null)
            throw new KeyNotFoundException("User account not found.");

        return await _depositRepository.ProcessDepositAsync(userId, request.Amount, request.Currency);
    }
}
```

#### 2. Modernized Clean Controller
```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CryptoTrading.Business.Services;
using CryptoTrading.Models.Requests;

namespace CryptoTrading.Core.Controllers;

[Route("api/deposits")]
[Authorize]
public class DepositsController : BaseApiController
{
    private readonly IFinancialService _financialService;

    public DepositsController(IFinancialService financialService)
    {
        _financialService = financialService;
    }

    [HttpPost]
    public async Task<IActionResult> Deposit([FromBody] DepositRequest request)
    {
        if (request == null)
            return BadRequest(new { success = false, message = "Payload cannot be empty.", errorCode = "INVALID_ARGUMENT" });

        int userId = CurrentUserId;
        
        // Delegate all orchestration, validation, and database operations to the service layer
        var depositResult = await _financialService.ProcessDepositAsync(userId, request);

        return EnvelopeOk(depositResult, "Deposit processed successfully.");
    }
}
```

---

## 5. JSON Serialization Configuration in `Program.cs`

To prevent payload mapping breaks in the React client, JSON output MUST preserve casing structures identical to legacy setups. Register modern controllers in `Program.cs` configured with camelCase properties:

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
app.MapControllers();
```

---

## 6. AI Vertical Slice Verification Checklist

Before certifying the migration of a single target controller slice, the AI shall confirm:

1. **Compilation Check:** Run `dotnet build` from the modern project root. Confirm exactly 0 compilation errors or blocking warnings.
2. **Contract Fidelity Verification:** Run comparative reviews to ensure there are no modifications to payload model shapes, response keys, or nested parameters.
3. **No Direct Repository Violations:** Check the migrated target controller code. Verify that NO data repository types (`IRepository`, database contexts, or connection strings) are imported or injected.
4. **Exception Integrity:** Verify that any business exceptions thrown by services (e.g., `ArgumentException`, `KeyNotFoundException`) are handled by global middleware/filters and mapped to uniform failure envelopes:
   ```json
   {
     "success": false,
     "message": "Error message description...",
     "errorCode": "ERROR_CODE"
   }
   ```
5. **No Collateral Modifications:** Confirm that only the specified controller and its explicit downstream vertical dependency slice were modified or created. No unrelated code files should be staged or changed.
