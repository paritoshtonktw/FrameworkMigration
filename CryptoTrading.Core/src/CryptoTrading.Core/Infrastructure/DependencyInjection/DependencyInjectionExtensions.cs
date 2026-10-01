using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using CryptoTrading.Data;
using CryptoTrading.Data.Repositories;
using CryptoTrading.Business.Services;
using CryptoTrading.Infrastructure.Security;
using CryptoTrading.Infrastructure.MarketData;
using CryptoTrading.Infrastructure.Sqs;
using CryptoTrading.Infrastructure.Reports;

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
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<ICryptocurrencyRepository, CryptocurrencyRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<ITradingRepository, TradingRepository>();
        services.AddScoped<IWalletRepository, WalletRepository>();
        services.AddScoped<IDepositRepository, DepositRepository>();
        services.AddScoped<IWithdrawalRepository, WithdrawalRepository>();
        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<IPortfolioRepository, PortfolioRepository>();
        return services;
    }

    public static IServiceCollection AddCoreServices(this IServiceCollection services)
    {
        services.AddMemoryCache();
        
        // Add service registrations here as vertical slices are migrated.
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ICryptoService, CryptoService>();
        services.AddScoped<ISqsPublisher, SqsPublisher>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<ITradingService, TradingService>();
        services.AddScoped<IFinancialService, FinancialService>();
        services.AddScoped<IPortfolioService, PortfolioService>();
        services.AddScoped<IPdfReportService, PdfReportService>();
        services.AddScoped<IOrderExecutionProcessor, OrderExecutionProcessor>();
        services.AddTransient<Func<ITradingService>>(sp => () => sp.GetRequiredService<ITradingService>());

        services.AddHttpClient<ICryptoMarketService, CoinGeckoMarketService>(client =>
        {
            client.BaseAddress = new Uri("https://api.coingecko.com/api/v3/");
            client.Timeout = TimeSpan.FromSeconds(10);
            client.DefaultRequestHeaders.Add("User-Agent", "CryptoTradingPlatform/1.0 (.NET10)");
            client.DefaultRequestHeaders.Add("Accept", "application/json");
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
