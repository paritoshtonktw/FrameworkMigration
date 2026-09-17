using System.ComponentModel.DataAnnotations;

namespace CryptoTrading.Models.Requests;

public class LoginRequest
{
    [Required]
    public string UsernameOrEmail { get; set; } = null!;

    [Required]
    public string Password { get; set; } = null!;
}
