namespace CryptoTrading.Models.Enums;

public enum OrderType
{
    Market,
    Limit
}

public enum OrderSide
{
    BUY,
    SELL
}

public enum OrderStatus
{
    Pending,
    Executed,
    Cancelled,
    Rejected
}

public enum TransactionType
{
    BUY,
    SELL,
    DEPOSIT,
    WITHDRAWAL
}

public enum TransactionStatus
{
    COMPLETED,
    PENDING,
    FAILED
}
