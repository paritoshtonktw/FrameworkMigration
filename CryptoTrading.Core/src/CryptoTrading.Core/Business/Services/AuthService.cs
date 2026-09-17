using System;
using System.Threading.Tasks;
using CryptoTrading.Data.Repositories;
using CryptoTrading.Infrastructure.Logging;
using CryptoTrading.Infrastructure.Security;
using CryptoTrading.Models.DTOs;
using CryptoTrading.Models.Entities;
using CryptoTrading.Models.Requests;

namespace CryptoTrading.Business.Services;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<UserProfileDto?> GetCurrentUserAsync(int userId);
}

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepo;
    private readonly IAccountRepository _accountRepo;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly ILoggerService _logger;

    public AuthService(
        IUserRepository userRepo,
        IAccountRepository accountRepo,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        ILoggerService logger)
    {
        _userRepo = userRepo;
        _accountRepo = accountRepo;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _logger = logger;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        // Validate existing username
        var existingUser = await _userRepo.GetUserByUsernameAsync(request.Username.Trim());
        if (existingUser != null)
        {
            throw new InvalidOperationException("Username is already taken.");
        }

        // Validate existing email
        var existingEmail = await _userRepo.GetUserByEmailAsync(request.Email.Trim());
        if (existingEmail != null)
        {
            throw new InvalidOperationException("Email address is already registered.");
        }

        // Hash password
        var passwordHash = _passwordHasher.HashPassword(request.Password);

        var user = await _userRepo.CreateUserAsync(
            request.Username.Trim(),
            request.Email.Trim(),
            passwordHash,
            request.FirstName.Trim(),
            request.LastName.Trim()
        );

        if (user == null)
        {
            throw new InvalidOperationException("User creation failed.");
        }

        _logger.Info($"User registered successfully: {user.Username} (UserId: {user.UserId})");

        var token = _tokenService.GenerateToken(user);

        return new AuthResponse
        {
            Token = token,
            TokenType = "Bearer",
            ExpiresIn = 86400,
            User = MapUserDto(user)
        };
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        var identifier = request.UsernameOrEmail.Trim();
        User? user = null;

        if (identifier.Contains("@"))
        {
            user = await _userRepo.GetUserByEmailAsync(identifier);
        }
        else
        {
            user = await _userRepo.GetUserByUsernameAsync(identifier);
        }

        if (user == null || !user.IsActive)
        {
            _logger.Warn($"Failed login attempt for identifier: {identifier}");
            throw new UnauthorizedAccessException("Invalid username/email or password.");
        }

        bool isValid = _passwordHasher.VerifyPassword(request.Password, user.PasswordHash);
        if (!isValid)
        {
            _logger.Warn($"Failed login attempt (password mismatch) for user: {user.Username}");
            throw new UnauthorizedAccessException("Invalid username/email or password.");
        }

        await _userRepo.UpdateLastLoginAsync(user.UserId);
        _logger.Info($"User logged in successfully: {user.Username} (UserId: {user.UserId})");

        var token = _tokenService.GenerateToken(user);

        return new AuthResponse
        {
            Token = token,
            TokenType = "Bearer",
            ExpiresIn = 86400,
            User = MapUserDto(user)
        };
    }

    public async Task<UserProfileDto?> GetCurrentUserAsync(int userId)
    {
        var user = await _userRepo.GetUserByIdAsync(userId);
        if (user == null)
            return null;

        var account = await _accountRepo.GetAccountAsync(userId, "USD");
        var balance = account?.AvailableBalance ?? 0m;
        var accountId = account?.AccountId ?? userId;

        return new UserProfileDto
        {
            UserId = user.UserId,
            Username = user.Username,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            CreatedDate = user.CreatedDate,
            LastLoginDate = user.LastLoginDate,
            CashBalance = balance,
            AccountId = accountId,
            Currency = account?.Currency ?? "USD"
        };
    }

    private static UserDto MapUserDto(User user)
    {
        return new UserDto
        {
            UserId = user.UserId,
            Username = user.Username,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            CreatedDate = user.CreatedDate,
            LastLoginDate = user.LastLoginDate
        };
    }
}
