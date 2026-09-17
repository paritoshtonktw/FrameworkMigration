# Migration Instructions: .NET Framework 4.7.2 to .NET 10 (Headless Controller-Based Clean Architecture)

This document provides precise, step-by-step instructions for an AI agent to initialize and scaffold a modernized **.NET 10** backend project for the CryptoTrading platform. 

The target design is a **Controller-based, headless clean architecture structure**. All API endpoints shall be represented as Web API Controllers inheriting from a common `BaseApiController`, completely bypassing Minimal APIs and adhering to strict Clean Architecture separations.

---

## 1. Directory Structure Blueprint

The AI shall scaffold the new project directory named `CryptoTrading.Core` alongside the existing project with the following clean architecture. Note that models, repositories, and services directories will be created as empty placeholder folders first, and then populated incrementally during individual vertical slice controller migrations:

```text
CryptoTrading.Core/
├── CryptoTrading.Core.csproj
├── Program.cs
├── appsettings.json
├── appsettings.Development.json
├── Properties/
│   └── launchSettings.json
├── Controllers/                         <-- Target directory for modernized Controllers
│   └── BaseApiController.cs             <-- Common base controller with JWT parsing & response envelope
├── Models/                              <-- Empty placeholder folders (populated during vertical slices)
│   ├── DTOs/
│   ├── Entities/
│   ├── Enums/
│   └── Requests/
├── Data/
│   ├── IDbConnectionFactory.cs          <-- Modern interface
│   ├── SqlConnectionFactory.cs          <-- High-performance database factory
│   └── Repositories/                    <-- Empty placeholder folder
├── Business/
│   └── Services/                        <-- Empty placeholder folder
└── Infrastructure/
    ├── Caching/
    ├── DependencyInjection/
    │   └── DependencyInjectionExtensions.cs
    ├── Logging/
    ├── MarketData/
    ├── PubSub/
    └── Security/
```

---

## 2. Step 1: Project Initialization

The AI shall execute the following command in the workspace directory to initialize a new web application targeting .NET 10:

```bash
dotnet new web -n CryptoTrading.Core -f net10.0
```

---

## 3. Step 2: Configure the .csproj File

Replace the generated `CryptoTrading.Core.csproj` with this streamlined, modern XML configuration. It pulls in modern .NET 10 package versions and enables global nullable context and modern C# implicit usings:

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <InvariantGlobalization>false</InvariantGlobalization>
  </PropertyGroup>

  <ItemGroup>
    <!-- Modern Database Driver -->
    <PackageReference Include="Microsoft.Data.SqlClient" Version="6.0.0-*" />

    <!-- Micro-ORM for High-Performance Mapping -->
    <PackageReference Include="Dapper" Version="2.1.*" />

    <!-- Modern Security and Identity -->
    <PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="10.0.0-*" />
    <PackageReference Include="System.IdentityModel.Tokens.Jwt" Version="8.0.0-*" />

    <!-- Enterprise-grade Caching and Helpers -->
    <PackageReference Include="Microsoft.Extensions.Caching.Memory" Version="10.0.0-*" />

    <!-- High-Performance Logger -->
    <PackageReference Include="Serilog.AspNetCore" Version="8.0.0-*" />

    <!-- Open API Documentation Support -->
    <PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="10.0.0-*" />
    
    <!-- Modern Resilience and Transients (e.g. for external services) -->
    <PackageReference Include="Microsoft.Extensions.Http.Resilience" Version="10.0.0-*" />
  </ItemGroup>

</Project>
```

---

## 4. Step 3: Create a High-Performance Data Access Layer (DAL) using Dapper

To eliminate legacy database-to-object mapping boilerplate, the AI shall implement a **strongly-typed Data Access Layer (DAL) powered by Dapper**. All repositories shall utilize Dapper extension methods on the standard `IDbConnection` provided by `IDbConnectionFactory`. Note: individual repositories are part of the vertical-slice controller migrations and are not scaffolded in the initial core structure step.

### 4.1 Core DAL Architecture & Standards
- **Strict Stored Procedure Integration:** Direct queries via dynamic SQL are forbidden. All operations MUST execute one of the 28 SQL Server Stored Procedures using `CommandType.StoredProcedure`.
- **Zero Raw DataTables:** The legacy use of `SqlDataAdapter`, `DataTable`, and `DataSet` is entirely banned. Dapper shall automatically deserialize result sets into strongly-typed DTOs/Entities.
- **Asynchronous Execution:** All data-access paths MUST use Dapper's async APIs (`QueryAsync`, `QueryFirstOrDefaultAsync`, `ExecuteAsync`, `QueryMultipleAsync`).
- **Connection Scope Management:** Always wrap the connection retrieval in a C# `using` statement to guarantee clean disposal and prevent connection pool leaks.

### 4.2 Code Blueprint: Base Dapper Repository Pattern
The AI shall follow this unified pattern across all repositories:

```csharp
using System.Data;
using Dapper;
using CryptoTrading.Data;
using CryptoTrading.Models.DTOs;
using CryptoTrading.Models.Requests;

namespace CryptoTrading.Data.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly IDbConnectionFactory _dbConnectionFactory;

    public OrderRepository(IDbConnectionFactory dbConnectionFactory)
    {
        _dbConnectionFactory = dbConnectionFactory;
    }

    /// <summary>
    /// Executes a Stored Procedure modifying data and returns a single strongly-typed result.
    /// </summary>
    public async Task<OrderDto> CreateOrderAsync(int userId, CreateOrderRequest request)
    {
        using var connection = _dbConnectionFactory.CreateConnection();

        var parameters = new DynamicParameters();
        parameters.Add("@UserId", userId, DbType.Int32);
        parameters.Add("@CryptocurrencyId", request.CryptocurrencyId, DbType.Int32);
        parameters.Add("@OrderType", request.OrderType, DbType.String, size: 10);
        parameters.Add("@Side", request.Side, DbType.String, size: 4);
        parameters.Add("@Quantity", request.Quantity, DbType.Decimal);
        parameters.Add("@Price", request.Price, DbType.Decimal);

        var order = await connection.QueryFirstOrDefaultAsync<OrderDto>(
            "usp_CreateOrder",
            parameters,
            commandType: CommandType.StoredProcedure
        );

        return order ?? throw new InvalidOperationException("Failed to register order.");
    }

    /// <summary>
    /// Streams large collections of data using high-performance C# IAsyncEnumerable.
    /// </summary>
    public async Task<IEnumerable<OrderDto>> GetOrdersByUserAsync(int userId, int limit)
    {
        using var connection = _dbConnectionFactory.CreateConnection();

        var parameters = new { UserId = userId, Limit = limit };

        // Dapper handles parameters and deserialization with zero boilerplate
        var orders = await connection.QueryAsync<OrderDto>(
            "usp_GetOrdersByUser",
            parameters,
            commandType: CommandType.StoredProcedure
        );

        return orders;
    }
}
```

### 4.3 Copying and Adapting Core Repositories
The AI shall migrate all database wrappers utilizing Dapper mappings:
1. **UserRepository:** Call `usp_GetUserById` and `usp_CreateUser` mapping output to `UserDto` using `QueryFirstOrDefaultAsync<UserDto>`.
2. **AccountRepository:** Call `usp_GetAccount` using Dapper's anonymous parameters parameter injection.
3. **TradingRepository:** Call atomic execution procedures (`usp_ExecuteBuyOrder`, `usp_ExecuteSellOrder`) within Dapper. Ensure returned rows containing trades are mapped straight to `TradeDto`.

### 4.4 Database Connection Pooling & Lifecycle Management
To maximize throughput and prevent connection pool exhaustion under heavy trading volumes, the AI shall implement the following connection pooling rules:

- **Connection Pool Configuration:** The `CryptoTradingDB` connection string MUST specify parameters fine-tuning connection recycling:
  - `Pooling=True;` (Enables native connection pooling)
  - `Min Pool Size=10;` (Maintains 10 pre-heated, idle database connections to eliminate handshake overhead)
  - `Max Pool Size=100;` (Restricts maximum concurrent active connections to protect database resources)
  - `Connection Timeout=30;` (Ensures web requests timeout gracefully if pool capacity is fully exhausted)
- **Singleton Connection Factory:** Register `IDbConnectionFactory` as a **Singleton** service in `Program.cs`. This ensures that a single, centralized connection pool is shared globally across all threads and HTTP requests.
- **No Class-Level Caching:** Repositories must NEVER store open `IDbConnection` instances in class-level fields. Connections must be fully transient and scoped to individual database operations.
- **Pessimistic "Just-In-Time" Open/Close Patterns:** Ensure connections are opened as late as possible, and closed as early as possible. Every database call MUST use a scoped `using var connection` block to guarantee automatic closure and immediate return of connection instances to the pool:

```csharp
public async Task<AccountDto?> GetAccountByUserIdAsync(int userId)
{
    // Scoped connection returned immediately to the pool at the end of this block
    using var connection = _dbConnectionFactory.CreateConnection();
    
    return await connection.QueryFirstOrDefaultAsync<AccountDto>(
        "usp_GetAccountByUserId",
        new { UserId = userId },
        commandType: CommandType.StoredProcedure
    );
}
```

---

## 5. Step 4: Configure Kestrel and Startup Config (`appsettings.json`)

Scaffold a clean configuration mapping the legacy settings into a standardized modern schema:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "Serilog": {
    "WriteTo": [
      { "Name": "Console" },
      { "Name": "File", "Args": { "path": "Logs/cryptotrading_core-.log", "rollingInterval": "Day" } }
    ]
  },
  "ConnectionStrings": {
    "CryptoTradingDB": "Server=(localdb)\\CryptoTradingDB;Database=CryptoTradingDB;Integrated Security=True;TrustServerCertificate=True;Encrypt=True;Pooling=True;Min Pool Size=10;Max Pool Size=100;Connection Timeout=30;"
  },
  "Jwt": {
    "SecretKey": "SuperSecretKeyForJWTAuthReplaceInProductionSecurely2026!",
    "Issuer": "CryptoTradingCore",
    "Audience": "CryptoTradingReact",
    "ExpiryHours": 24
  },
  "PubSub": {
    "Enabled": false,
    "ProjectId": "mock-gcp-project",
    "EmulatorHost": "localhost:8085"
  },
  "AllowedHosts": "*"
}
```

---

## 6. Step 5: Implement `Program.cs` (DI, Services & Controller Routing)

The AI shall construct a consolidated `Program.cs` configuring the core hosting context, service pipeline, security filters, and MVC Controller routing maps. 

All database, security, and domain service dependencies shall be cleanly registered using custom extension methods, keeping the bootstrap code simple, clean, and elegant.

*Crucial Requirement:* **Web API Controllers must be registered with JSON options enforcing camelCase serialization to support the React SPA frontend.**

```csharp
using Serilog;
using CryptoTrading.Core.Infrastructure.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// 1. Configure Serilog for Structured Logging
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseSerilog();

// 2. Register Web API Controllers with camelCase serialization
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DictionaryKeyPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

// 3. Register Open API / Swagger Support
builder.Services.AddOpenApi();

// 4. Register Core Application Layers using decoupled Extension Methods
builder.Services
    .AddCoreDatabase(builder.Configuration)
    .AddCoreRepositories()
    .AddCoreServices()
    .AddCoreAuthentication(builder.Configuration);

// 5. Configure Modern CORS Matching the React Web App Origin
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:5173") // React Dev Ports
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// 6. Global Error-Handling Middleware (Envelope-compliant API Errors)
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Unhandled exception occurred while processing request {Path}", context.Request.Path);
        
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;

        var errorEnvelope = new
        {
            success = false,
            message = "An unexpected error occurred on the server.",
            errorCode = "INTERNAL_SERVER_ERROR"
        };

        await context.Response.WriteAsJsonAsync(errorEnvelope);
    }
});

// 7. Pipeline Routing Setup
app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

// 8. Swagger / OpenAPI Endpoint Routing
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// 9. Map Web API Controllers
app.MapControllers();

app.Run();
```

---

## 6.1 Step 5.1: Create Dependency Injection Class (`DependencyInjectionExtensions.cs`)

The AI shall scaffold a dedicated Dependency Injection file containing extensions on `IServiceCollection` to manage registrations modularly. Methods for repositories and services are registered as empty shells in this core setup phase and are populated incrementally as vertical slices are migrated:

```csharp
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using CryptoTrading.Data;

namespace CryptoTrading.Core.Infrastructure.DependencyInjection;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddCoreDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("CryptoTradingDB") 
            ?? throw new InvalidOperationException("Connection string 'CryptoTradingDB' is missing.");

        services.AddSingleton<IDbConnectionFactory>(sp => new SqlConnectionFactory(connectionString));
        return services;
    }

    public static IServiceCollection AddCoreRepositories(this IServiceCollection services)
    {
        // Add repository registrations here as vertical slices are migrated.
        return services;
    }

    public static IServiceCollection AddCoreServices(this IServiceCollection services)
    {
        services.AddMemoryCache();
        // Add service registrations here as vertical slices are migrated.
        return services;
    }

    public static IServiceCollection AddCoreAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtKey = configuration["Jwt:SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey is missing.");
        var keyBytes = Encoding.UTF8.GetBytes(jwtKey);

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = configuration["Jwt:Issuer"],
                ValidAudience = configuration["Jwt:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
                ClockSkew = TimeSpan.Zero
            };
        });

        services.AddAuthorization();
        return services;
    }
}
```

---

## 7. Step 6: Create Base API Controller (`BaseApiController.cs`)

To automate claims extraction and enforce the uniform response envelope structure across all endpoints, the AI shall scaffold a common `BaseApiController` class in the `Controllers/` directory:

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

## 8. Architectural Transformation Guidelines for Vertical Slices

When migrating individual vertical slices (including models, repositories, business services, and controllers), the AI shall follow these structural design patterns:

### 8.1 Typed HttpClients & Polly Resilience (e.g., CoinGecko Integration)
External HTTP API clients (such as `CoinGeckoMarketService`) must be typed and configured with Polly resilience pipelines (retry mechanics, circuit breaker) in `DependencyInjectionExtensions.cs` using `Microsoft.Extensions.Http.Resilience`.

### 8.2 Hosted Background Services (e.g., GCP Pub/Sub Integration)
Long-running asynchronous consumers (such as `PubSubBackgroundSubscriber`) must inherit from `BackgroundService` and register as lifecycle-managed hosted services:
```csharp
services.AddHostedService<PubSubBackgroundSubscriber>();
```

---

## 9. Step 7: AI Verification Checklist, Build, and Startup Validation

Before declaring the core project structure setup complete, the AI shall run and verify:

1. **Add Swagger UI Middleware Package:**
   Ensure the following package is installed for visual Swagger UI page rendering:
   ```bash
   dotnet add package Swashbuckle.AspNetCore.SwaggerUi
   ```
2. **Configure Swagger UI Route in `Program.cs`:**
   Verify `Program.cs` enables the Swagger UI router pointing to native OpenAPI specs in development:
   ```csharp
   if (app.Environment.IsDevelopment())
   {
       app.MapOpenApi();
       app.UseSwaggerUI(options =>
       {
           options.SwaggerEndpoint("/openapi/v1.json", "CryptoTrading Core API v1");
           options.RoutePrefix = "swagger";
       });
   }
   ```
3. **Configure Startup Launch Page:**
   Confirm `Properties/launchSettings.json` sets `"launchUrl": "swagger"` on profiles so the browser opens directly to Swagger on startup:
   ```json
   "launchUrl": "swagger"
   ```
4. **Compilable Core:** Execute `dotnet build` from the `CryptoTrading.Core/` directory. Ensure there are 0 compilation errors.
5. **Startup Health Check:**
   - Execute the project in the background (`dotnet run` or equivalent task).
   - Perform an HTTP head request to verify that the Swagger UI is fully responsive and serving pages correctly on port `5152` or `7164`:
     ```bash
     curl -s -I http://localhost:5152/swagger/index.html
     ```
   - Confirm the endpoint returns a successful status code:
     ```http
     HTTP/1.1 200 OK
     ```
6. **Controller Routing Verification:** Confirm `AddControllers()` and `MapControllers()` are correctly specified in `Program.cs` and that no Minimal API endpoint route groupings exist.
7. **Common Base Controller:** Confirm that `BaseApiController` exists in the `Controllers/` directory with correct namespace, authorization claims extraction helper, and uniform JSON envelope methods.
8. **Clean Project Structure:** Check that placeholder folders for `Models`, `Data/Repositories`, and `Business/Services` are scaffolded but left unpopulated for future vertical slices.

