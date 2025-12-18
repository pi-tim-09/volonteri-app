namespace WebApp.Common
{
    /// <summary>
    /// Standardized API response wrapper for consistent client communication
    /// Follows Single Responsibility Principle - handles only response structure
    /// </summary>
    /// <typeparam name="T">Type of data being returned</typeparam>
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public T? Data { get; set; }
        public List<string>? Errors { get; set; }

        public ApiResponse()
        {
        }

        public ApiResponse(T data, string? message = null)
        {
            Success = true;
            Data = data;
            Message = message;
        }

        public ApiResponse(string message, List<string>? errors = null)
        {
            Success = false;
            Message = message;
            Errors = errors;
        }

        /// <summary>
        /// Creates a successful response with data
        /// </summary>
        public static ApiResponse<T> SuccessResponse(T data, string? message = null)
        {
            return new ApiResponse<T>(data, message);
        }

        /// <summary>
        /// Creates an error response
        /// </summary>
        public static ApiResponse<T> ErrorResponse(string message, List<string>? errors = null)
        {
            return new ApiResponse<T>(message, errors);
        }
    }

    /// <summary>
    /// Non-generic version for responses without data
    /// </summary>
    public class ApiResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public List<string>? Errors { get; set; }

        public ApiResponse()
        {
        }

        public ApiResponse(bool success, string? message = null, List<string>? errors = null)
        {
            Success = success;
            Message = message;
            Errors = errors;
        }

        public static ApiResponse SuccessResponse(string? message = null)
        {
            return new ApiResponse(true, message);
        }

        public static ApiResponse ErrorResponse(string message, List<string>? errors = null)
        {
            return new ApiResponse(false, message, errors);
        }
    }
}
