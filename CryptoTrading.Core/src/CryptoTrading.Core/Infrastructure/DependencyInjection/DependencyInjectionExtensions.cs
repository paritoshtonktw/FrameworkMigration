using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using CryptoTrading.Data;
using CryptoTrading.Data.Repositories;
using CryptoTrading.Business.Services;
using CryptoTrading.Infrastructure.Security;
using CryptoTrading.Infrastructure.Logging;
using CryptoTrading.Infrastructure.MarketData;
using CryptoTrading.Infrastructure.PubSub;

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
        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<IDepositRepository, DepositRepository>();
        services.AddScoped<IWithdrawalRepository, WithdrawalRepository>();
        services.AddScoped<ICryptocurrencyRepository, CryptocurrencyRepository>();
        services.AddScoped<ITradingRepository, TradingRepository>();
        services.AddScoped<IWalletRepository, WalletRepository>();
        services.AddScoped<IPortfolioRepository, PortfolioRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        return services;
    }

    public static IServiceCollection AddCoreServices(this IServiceCollection services)
    {
        services.AddMemoryCache();
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddSingleton<ILoggerService, SerilogLoggerService>();
        services.AddSingleton<IPubSubPublisher, PubSubPublisher>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IFinancialService, FinancialService>();
        services.AddScoped<ICryptoService, CryptoService>();
        services.AddScoped<ITradingService, TradingService>();
        services.AddScoped<IPortfolioService, PortfolioService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IOrderExecutionProcessor, OrderExecutionProcessor>();
        services.AddTransient<Func<ITradingService>>(sp => () => sp.GetRequiredService<ITradingService>());

        services.AddHttpClient<ICryptoMarketService, CoinGeckoMarketService>(client =>
        {
            client.BaseAddress = new Uri("https://api.coingecko.com/api/v3/");
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            client.DefaultRequestHeaders.Add("User-Agent", "CryptoTradingPlatform/1.0 (.NETCore10)");
            client.Timeout = TimeSpan.FromSeconds(15);
        })
        .AddStandardResilienceHandler(options =>
        {
            options.Retry.BackoffType = Polly.DelayBackoffType.Exponential;
            options.Retry.MaxRetryAttempts = 3;
            options.Retry.Delay = TimeSpan.FromSeconds(2);

            options.CircuitBreaker.FailureRatio = 0.5;
            options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
            options.CircuitBreaker.MinimumThroughput = 8;
            options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(15);
        });

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
