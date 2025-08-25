namespace ZefsjulaApi.Models.DTO
{
    public class ErrorResponse
    {
        public bool Success { get; set; } = false;
        public string Message { get; set; } = string.Empty;
        public string? ErrorCode { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string? TraceId { get; set; }
        public Dictionary<string, object>? Details { get; set; }
        public Dictionary<string, string[]>? ValidationErrors { get; set; }

        public static ErrorResponse Create(string message, string? errorCode = null, string? traceId = null)
        {
            return new ErrorResponse
            {
                Message = message,
                ErrorCode = errorCode,
                TraceId = traceId
            };
        }

        public static ErrorResponse CreateValidation(Dictionary<string, string[]> validationErrors, string? traceId = null)
        {
            return new ErrorResponse
            {
                Message = "Validation failed",
                ErrorCode = "VALIDATION_ERROR",
                ValidationErrors = validationErrors,
                TraceId = traceId
            };
        }
    }
}
