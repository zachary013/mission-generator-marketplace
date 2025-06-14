namespace SmartMarketplace.Models;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? ErrorMessage { get; set; }
    public string? Provider { get; set; } // Which AI provider was used
    
    public static ApiResponse<T> SuccessResult(T data, string? provider = null)
    {
        return new ApiResponse<T>
        {
            Success = true,
            Data = data,
            Provider = provider
        };
    }
    
    public static ApiResponse<T> ErrorResult(string errorMessage)
    {
        return new ApiResponse<T>
        {
            Success = false,
            ErrorMessage = errorMessage
        };
    }
}
