using System;

namespace CryptoTrading.Models.DTOs;

public class OrderDto
{
    public int OrderId { get; set; }
    public int UserId { get; set; }
    public string Symbol { get; set; } = null!;
    public string OrderType { get; set; } = null!;
    public string Side { get; set; } = null!;
    public decimal Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal ExecutedPrice => Price;
    public decimal TotalValue { get; set; }
    public decimal TotalAmount => TotalValue;
    public string Status { get; set; } = null!;
    public DateTime CreatedDate { get; set; }
    public DateTime? ExecutedDate { get; set; }
}
