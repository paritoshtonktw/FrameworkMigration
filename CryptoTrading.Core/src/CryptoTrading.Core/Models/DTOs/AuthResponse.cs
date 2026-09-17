namespace CryptoTrading.Models.DTOs;

public class AuthResponse
{
    public string Token { get; set; } = null!;
    public string TokenType { get; set; } = "Bearer";
    public int ExpiresIn { get; set; }
    public UserDto User { get; set; } = null!;
}
