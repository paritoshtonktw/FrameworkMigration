namespace CryptoTrading.Models.Requests;

public class ApiErrorResponse
{
    public bool Success { get; set; } = false;
    public string? Message { get; set; }
    public string? ErrorCode { get; set; }

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
