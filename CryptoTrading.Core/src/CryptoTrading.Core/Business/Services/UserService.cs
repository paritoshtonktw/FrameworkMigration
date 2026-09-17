using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CryptoTrading.Data.Repositories;
using CryptoTrading.Models.DTOs;
using CryptoTrading.Models.Requests;

namespace CryptoTrading.Business.Services;

public interface IUserService
{
    Task<UserProfileDto> GetUserProfileAsync(int userId);
    Task<UserProfileDto> UpdateUserProfileAsync(int userId, UpdateProfileRequest request);
}

public class UserService : IUserService
{
    private readonly IUserRepository _userRepo;
    private readonly IAccountRepository _accountRepo;

    public UserService(IUserRepository userRepo, IAccountRepository accountRepo)
    {
        _userRepo = userRepo;
        _accountRepo = accountRepo;
    }

    public async Task<UserProfileDto> GetUserProfileAsync(int userId)
    {
        var user = await _userRepo.GetUserByIdAsync(userId);
        if (user == null)
            throw new KeyNotFoundException($"User #{userId} was not found.");

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

    public async Task<UserProfileDto> UpdateUserProfileAsync(int userId, UpdateProfileRequest request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        var updatedUser = await _userRepo.UpdateUserAsync(
            userId,
            request.FirstName.Trim(),
            request.LastName.Trim(),
            request.Email.Trim()
        );

        if (updatedUser == null)
            throw new InvalidOperationException("Failed to update profile.");

        var account = await _accountRepo.GetAccountAsync(userId, "USD");
        var balance = account?.AvailableBalance ?? 0m;
        var accountId = account?.AccountId ?? userId;

        return new UserProfileDto
        {
            UserId = updatedUser.UserId,
            Username = updatedUser.Username,
            Email = updatedUser.Email,
            FirstName = updatedUser.FirstName,
            LastName = updatedUser.LastName,
            CreatedDate = updatedUser.CreatedDate,
            LastLoginDate = updatedUser.LastLoginDate,
            CashBalance = balance,
            AccountId = accountId,
            Currency = account?.Currency ?? "USD"
        };
    }
}
