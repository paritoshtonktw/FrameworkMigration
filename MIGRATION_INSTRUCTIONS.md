# Migration Instructions: .NET Framework 4.7.2 to .NET 10 (Headless Service-Only Architecture)

This document provides precise, step-by-step instructions for an AI agent to initialize and scaffold a modernized **.NET 10** backend project for the CryptoTrading platform. 

The target design is a **Controller-less, headless domain-and-service-only structure**. All API endpoints shall be represented as high-performance **Minimal API Endpoints** mapped directly inside `Program.cs` or dedicated endpoint group mappers, completely bypassing traditional MVC Controllers (`ControllerBase`).

---

## 1. Directory Structure Blueprint

The AI shall scaffold the new project directory named `CryptoTrading.Core` alongside the existing project with the following clean architecture:

```text
CryptoTrading.Core/
├── CryptoTrading.Core.csproj
├── Program.cs
├── appsettings.json
├── appsettings.Development.json
├── Properties/
│   └── launchSettings.json
├── Models/
│   ├── DTOs/
│   ├── Entities/
│   ├── Enums/
│   └── Requests/
├── Data/
│   ├── IDbConnectionFactory.cs
│   ├── SqlConnectionFactory.cs
│   └── Repositories/
│       ├── IAccountRepository.cs
│       ├── ICryptocurrencyRepository.cs
│       ├── IOrderRepository.cs
│       ├── IPortfolioRepository.cs
│       ├── ITradingRepository.cs
│       ├── IUserRepository.cs
│       └── IWalletRepository.cs
├── Business/
│   └── Services/
│       ├── IAuthService.cs
│       ├── ICryptoService.cs
│       ├── IFinancialService.cs
│       ├── IOrderService.cs
│       ├── IPortfolioService.cs
│       └── ITradingService.cs
├── Infrastructure/
│   ├── Caching/
│   ├── DependencyInjection/
│   │   └── DependencyInjectionExtensions.cs (For Service & Repository Registrations)
│   ├── Logging/
│   ├── MarketData/
│   ├── PubSub/
│   └── Security/
└── Endpoints/
    └── EndpointRouteBuilderExtensions.cs (For Minimal API Group Mappings)
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
<Project Project="Microsoft.NET.Sdk.Web">

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
  </ItemGroup>

</Project>
```

---

## 4. Step 3: Create a High-Performance Data Access Layer (DAL) using Dapper

To eliminate legacy database-to-object mapping boilerplate, the AI shall implement a **strongly-typed Data Access Layer (DAL) powered by Dapper**. All repositories shall utilize Dapper extension methods on the standard `IDbConnection` provided by `IDbConnectionFactory`.

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

Scaffold a clean configuration configuration mapping the legacy settings into a standardized modern schema:

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

## 6. Step 5: Implement `Program.cs` (DI, Services & Minimal Routing)

The AI shall construct a consolidated `Program.cs` configuring the core hosting context, service pipeline, security filters, and routing maps. 

All database, repository, security, and domain service dependencies shall be cleanly registered using custom extension methods (see Step 5.1), keeping the bootstrap code simple, clean, and elegant.

*Crucial Requirement:* **No traditional MVC controllers shall be registered (`AddControllers` and `MapControllers` are strictly forbidden).**

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

// 2. Register Open API / Swagger Support
builder.Services.AddOpenApi();

// 3. Register Core Application Layers using decoupled Extension Methods
builder.Services
    .AddCoreDatabase(builder.Configuration)
    .AddCoreRepositories()
    .AddCoreServices()
    .AddCoreAuthentication(builder.Configuration);

// 4. Configure Modern CORS Matching the React Web App Origin
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

// 5. Global Error-Handling Middleware (Envelope-compliant API Errors)
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

// 6. Pipeline Routing Setup
app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

// 7. Swagger / OpenAPI Endpoint Routing
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// 8. Minimal API Endpoint Route Group Mappings
var apiGroup = app.MapGroup("/api");

// Map sub-groups cleanly utilizing direct extensions (no Controllers)
apiGroup.MapAuthEndpoints();
apiGroup.MapCryptoEndpoints();
apiGroup.MapPortfolioEndpoints();
apiGroup.MapTradingEndpoints();

app.Run();
```

---

## 6.1 Step 5.1: Create Dependency Injection Class (`DependencyInjectionExtensions.cs`)

The AI shall scaffold a dedicated Dependency Injection file containing extensions on `IServiceCollection` to manage registrations modularly:

```csharp
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using CryptoTrading.Data;
using CryptoTrading.Data.Repositories;
using CryptoTrading.Business.Services;

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
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<ICryptocurrencyRepository, CryptocurrencyRepository>();
        services.AddScoped<IWalletRepository, WalletRepository>();
        services.AddScoped<IPortfolioRepository, PortfolioRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<ITradingRepository, TradingRepository>();
        return services;
    }

    public static IServiceCollection AddCoreServices(this IServiceCollection services)
    {
        services.AddMemoryCache();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICryptoService, CryptoService>();
        services.AddScoped<ITradingService, TradingService>();
        services.AddScoped<IPortfolioService, PortfolioService>();
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

## 7. Step 6: Endpoint Mapping Design Pattern

To keep `Program.cs` completely clean and free of route clutter, the AI shall implement the Minimal API route groupings in dedicated extension files using the pattern below:

```csharp
namespace CryptoTrading.Core.Endpoints;

public static class EndpointRouteBuilderExtensions
{
    public static RouteGroupBuilder MapAuthEndpoints(this RouteGroupBuilder group)
    {
        var auth = group.MapGroup("/auth");

        auth.MapPost("/register", async (RegisterRequest request, IAuthService authService) =>
        {
            var result = await authService.RegisterAsync(request);
            return Results.Created($"/api/profile/{result.User.UserId}", new { success = true, data = result });
        }).AllowAnonymous();

        auth.MapPost("/login", async (LoginRequest request, IAuthService authService) =>
        {
            var result = await authService.LoginAsync(request);
            return Results.Ok(new { success = true, data = result });
        }).AllowAnonymous();

        return group;
    }
}
```

---

## 8. Step 7: Resilient HttpClient Factory (External CoinGecko Integration)

The legacy `CoinGeckoMarketService` is highly vulnerable to connection exhaustion and HTTP 429 rate-limiting lockouts. To modernize this external integration, the AI shall implement **Typed HttpClients** backed by **`IHttpClientFactory`** and configured with a native **Resilience Pipeline (Polly)**:

1. **Add Resilience Packages:** The `.csproj` MUST include the standard resilience package:
   ```xml
   <PackageReference Include="Microsoft.Extensions.Http.Resilience" Version="10.0.0-*" />
   ```
2. **Configure HttpClient in DI Extensions:** Inside `DependencyInjectionExtensions.cs`, register the market service as a resilient typed HTTP client:
   ```csharp
   services.AddHttpClient<ICryptoMarketService, CoinGeckoMarketService>(client =>
   {
       client.BaseAddress = new Uri("https://api.coingecko.com/api/v3/");
       client.DefaultRequestHeaders.Add("Accept", "application/json");
       client.Timeout = TimeSpan.FromSeconds(15);
   })
   .AddStandardResilienceHandler(options =>
   {
       // 1. Configure standard retry mechanics for transient status codes (5xx, 429)
       options.Retry.BackoffType = Polly.DelayBackoffType.Exponential;
       options.Retry.MaxRetryAttempts = 3;
       options.Retry.Delay = TimeSpan.FromSeconds(2);

       // 2. Configure Circuit Breaker to prevent slamming downstream if API drops
       options.CircuitBreaker.FailureRatio = 0.5; // Trip if 50% of requests fail
       options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
       options.CircuitBreaker.MinimumThroughput = 8;
       options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(15);
   });
   ```
3. **Scaffold Resilient Service:** The typed client consumes the resilient `HttpClient` implicitly injected into its constructor:
   ```csharp
   using Microsoft.Extensions.Caching.Memory;
   using System.Net.Http.Json;

   namespace CryptoTrading.Core.Infrastructure.MarketData;

   public class CoinGeckoMarketService : ICryptoMarketService
   {
       private readonly HttpClient _httpClient;
       private readonly IMemoryCache _cache;
       private const string CacheKey = "MarketPrices";

       public CoinGeckoMarketService(HttpClient httpClient, IMemoryCache cache)
       {
           _httpClient = httpClient;
           _cache = cache;
       }

       public async Task<List<CryptoPriceDto>> GetMarketPricesAsync()
       {
           // Leverage in-memory sliding cache to respect rate-limits
           if (_cache.TryGetValue(CacheKey, out List<CryptoPriceDto>? cachedPrices))
           {
               return cachedPrices!;
           }

           try
           {
               // Built-in resilience handler handles retries, timeouts, and circuit breakers automatically
               var response = await _httpClient.GetFromJsonAsync<List<CryptoPriceDto>>("coins/markets?vs_currency=usd");
               if (response != null)
               {
                   _cache.Set(CacheKey, response, TimeSpan.FromSeconds(45));
                   return response;
               }
           }
           catch (Exception ex)
           {
               // Fall back gracefully to local database price tables if external API is locked or offline
               return await FallbackToLocalDatabasePricesAsync();
           }

           throw new InvalidOperationException("Market data currently unavailable.");
       }

       private Task<List<CryptoPriceDto>> FallbackToLocalDatabasePricesAsync()
       {
           // Call repositories/database to extract last cached values
           return Task.FromResult(new List<CryptoPriceDto>());
       }
   }
   ```

---

## 9. Step 8: Event-Driven Architecture with Background Hosted Services (`BackgroundService`)

The legacy Google Cloud Pub/Sub integration was unmanaged and manually bound to direct routing hubs. In .NET 10, all long-running asynchronous message processors and event streaming consumers MUST be managed as **Hosted Background Services** inheriting from **`BackgroundService`**:

1. **Scaffold Background Hosted Listener:** Create a background worker that launches with Kestrel startup, pulls messages asynchronously, and handles graceful cancellation tokens cleanly:
   ```csharp
   using Microsoft.Extensions.Hosting;
   using CryptoTrading.Infrastructure.PubSub;

   namespace CryptoTrading.Core.Infrastructure.BackgroundWorkers;

   public class PubSubBackgroundSubscriber : BackgroundService
   {
       private readonly IPubSubSubscriber _subscriber;
       private readonly ILogger<PubSubBackgroundSubscriber> _logger;
       private readonly IServiceProvider _serviceProvider;

       public PubSubBackgroundSubscriber(
           IPubSubSubscriber subscriber, 
           ILogger<PubSubBackgroundSubscriber> logger,
           IServiceProvider serviceProvider)
       {
           _subscriber = subscriber;
           _logger = logger;
           _serviceProvider = serviceProvider;
       }

       protected override async Task ExecuteAsync(CancellationToken stoppingToken)
       {
           _logger.LogInformation("Google Cloud Pub/Sub Background Subscriber is starting...");

           // Initialize the subscription thread
           _subscriber.Subscribe<OrderExecutionMessage>(
               "order-execution-sub", 
               async (orderingKey, message) =>
               {
                   _logger.LogDebug("Processing execution for Order: {OrderId}, Key: {Key}", message.OrderId, orderingKey);
                   
                   // Resolve scoped domain services safely inside background thread scope
                   using var scope = _serviceProvider.CreateScope();
                   var tradingService = scope.ServiceProvider.GetRequiredService<ITradingService>();

                   await tradingService.ProcessAsynchronousExecutionAsync(message);
               }, 
               stoppingToken
           );

           // Keep background thread alive while cancellation is not requested
           while (!stoppingToken.IsCancellationRequested)
           {
               await Task.Delay(1000, stoppingToken);
           }

           _logger.LogInformation("Google Cloud Pub/Sub Background Subscriber is stopping gracefully...");
           _subscriber.Stop();
       }
   }
   ```
2. **Register Hosted Service in DI Extension:**
   Add the background service to the collection inside `DependencyInjectionExtensions.cs` so its lifecycle is managed natively:
   ```csharp
   public static IServiceCollection AddCoreServices(this IServiceCollection services)
   {
       services.AddMemoryCache();
       services.AddScoped<IAuthService, AuthService>();
       services.AddScoped<ICryptoService, CryptoService>();
       services.AddScoped<ITradingService, TradingService>();
       services.AddScoped<IPortfolioService, PortfolioService>();

       // Register the Pub/Sub Subscriber Worker as a Hosted Lifecycle Service
       services.AddHostedService<PubSubBackgroundSubscriber>();

       return services;
   }
   ```

---

## 10. Step 9: AI Verification Checklist & Build Validation

Before declaring the migration of the application core complete, the AI shall run and verify:

1. **Compilable Codebase:** Execute `dotnet build` from the `CryptoTrading.Core/` directory. Ensure there are 0 compilation errors or blocking nullable warnings.
2. **Missing Controller Verification:** Inspect the built assembly (or files) to ensure **zero** references to Microsoft.AspNetCore.Mvc.ControllerBase are present, and no `/Controllers` directory exists.
3. **Resilient HTTP Client Verifications:** Verify that the Typed HttpClient handles transient errors gracefully, does not lock up under rate-limiting scenarios, and falls back to SQL cache properly.
4. **Graceful Worker Shutdown Tests:** Check that stopping the application sends correct cancellation signals to `PubSubBackgroundSubscriber` and closes GCP connections instantly with zero leakage.
5. **Database Connectivity Validation:** Verify the database connection string and query behavior.
6. **JWT Expiration & Issuance Audits:** Execute automated unit tests against the `IAuthService` logic to confirm robust token generation and claim mapping.

