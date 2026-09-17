using System.ComponentModel.DataAnnotations;

namespace CryptoTrading.Models.Requests;

public class BuyTradeRequest
{
    [Required]
    public string Symbol { get; set; } = null!;

    [Required]
    [Range(0.000001, 1000000.0)]
    public decimal Quantity { get; set; }

    public string OrderType { get; set; } = "MARKET"; // MARKET, LIMIT
}
