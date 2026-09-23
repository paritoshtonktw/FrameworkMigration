using System.ComponentModel.DataAnnotations;

namespace CryptoTrading.Models.Requests;

public class CreateOrderRequest
{
    [Required]
    public string Symbol { get; set; } = null!;

    [Required]
    public string Side { get; set; } = null!; // BUY, SELL

    [Required]
    public string OrderType { get; set; } = null!; // MARKET, LIMIT

    [Required]
    [Range(0.000001, 1000000.0)]
    public decimal Quantity { get; set; }

    public decimal? Price { get; set; } // Required if LIMIT order
}
