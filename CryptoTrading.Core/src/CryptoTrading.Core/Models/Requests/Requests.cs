using System;
using System.ComponentModel.DataAnnotations;
using CryptoTrading.Models.DTOs;

namespace CryptoTrading.Models.Requests
{
    public class RegisterRequest
    {
        [Required]
        [StringLength(50, MinimumLength = 3)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 6)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string LastName { get; set; } = string.Empty;
    }

    public class LoginRequest
    {
        [Required]
        public string UsernameOrEmail { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }

    public class AuthResponse
    {
        public string Token { get; set; } = string.Empty;
        public string TokenType { get; set; } = "Bearer";
        public int ExpiresIn { get; set; }
        public UserDto User { get; set; } = new UserDto();
    }

    public class BuyTradeRequest
    {
        [Required]
        public string Symbol { get; set; } = string.Empty;

        [Required]
        [Range(0.00000001, 1000000.0)]
        public decimal Quantity { get; set; }

        public string OrderType { get; set; } = "MARKET";
    }

    public class SellTradeRequest
    {
        [Required]
        public string Symbol { get; set; } = string.Empty;

        [Required]
        [Range(0.00000001, 1000000.0)]
        public decimal Quantity { get; set; }

        public string OrderType { get; set; } = "MARKET";
    }

    public class CreateOrderRequest
    {
        [Required]
        public string Symbol { get; set; } = string.Empty;

        [Required]
        public string OrderType { get; set; } = "MARKET"; // MARKET, LIMIT

        [Required]
        public string Side { get; set; } = string.Empty; // BUY, SELL

        [Required]
        [Range(0.00000001, 1000000.0)]
        public decimal Quantity { get; set; }

        public decimal? Price { get; set; }
    }

    public class DepositRequest
    {
        [Required]
        [Range(1.0, 10000000.0)]
        public decimal Amount { get; set; }

        public string Currency { get; set; } = "USD";
    }

    public class WithdrawalRequest
    {
        [Required]
        [Range(1.0, 10000000.0)]
        public decimal Amount { get; set; }

        public string Currency { get; set; } = "USD";
    }

    public class UpdateProfileRequest
    {
        [Required]
        [StringLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;
    }

    public class ApiResponse<T>
    {
        public bool Success { get; set; } = true;
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }

        public static ApiResponse<T> Ok(T data, string? message = null)
        {
            return new ApiResponse<T>
            {
                Success = true,
                Message = message ?? "Success",
                Data = data
            };
        }
    }

    public class ApiErrorResponse
    {
        public bool Success { get; set; } = false;
        public string Message { get; set; } = string.Empty;
        public string ErrorCode { get; set; } = "ERROR";

        public static ApiErrorResponse Fail(string message, string errorCode = "ERROR")
        {
            return new ApiErrorResponse
            {
                Success = false,
                Message = message,
                ErrorCode = errorCode
            };
        }
    }
}
