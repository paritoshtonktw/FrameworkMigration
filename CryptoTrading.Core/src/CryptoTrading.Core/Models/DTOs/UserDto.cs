using System;

namespace CryptoTrading.Models.DTOs;

public class UserDto
{
    public int UserId { get; set; }
    public string Username { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public DateTime CreatedDate { get; set; }
    public DateTime? LastLoginDate { get; set; }
}
