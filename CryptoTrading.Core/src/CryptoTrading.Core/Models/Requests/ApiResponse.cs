namespace CryptoTrading.Models.Requests;

public class ApiResponse<T>
{
    public bool Success { get; set; } = true;
    public string? Message { get; set; }
    public T? Data { get; set; }

    public static ApiResponse<T> Ok(T data, string? message = null)
    {
        return new ApiResponse<T>
        {
            Success = true,
            Message = message,
            Data = data
        };
    }
}
