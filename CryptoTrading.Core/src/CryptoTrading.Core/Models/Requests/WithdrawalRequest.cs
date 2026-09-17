using System.ComponentModel.DataAnnotations;

namespace CryptoTrading.Models.Requests;

public class WithdrawalRequest
{
    [Required]
    [Range(1.0, 10000000.0)]
    public decimal Amount { get; set; }

    public string Currency { get; set; } = "USD";
}
