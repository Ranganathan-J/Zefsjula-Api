using System.Net;
using System.Text.Json;
using ZefsjulaApi.Exceptions;
using ZefsjulaApi.Models.DTO;
using ZefsjulaApi.Models.Responses;

namespace ZefsjulaApi.Middleware
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;
        private readonly IWebHostEnvironment _environment;

        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger, IWebHostEnvironment environment)
        {
            _next = next;
            _logger = logger;
            _environment = environment;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception occurred. TraceId: {TraceId}", context.TraceIdentifier);
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var response = context.Response;
            response.ContentType = "application/json";

            var errorResponse = exception switch
            {
                NotFoundException ex => new ErrorResponse
                {
                    Message = ex.Message,
                    ErrorCode = "NOT_FOUND",
                    TraceId = context.TraceIdentifier
                },
                BadRequestException ex => new ErrorResponse
                {
                    Message = ex.Message,
                    ErrorCode = "BAD_REQUEST",
                    TraceId = context.TraceIdentifier
                },
                ValidationException ex => ErrorResponse.CreateValidation(ex.Errors, context.TraceIdentifier),
                ConflictException ex => new ErrorResponse
                {
                    Message = ex.Message,
                    ErrorCode = "CONFLICT",
                    TraceId = context.TraceIdentifier
                },
                UnauthorizedException ex => new ErrorResponse
                {
                    Message = ex.Message,
                    ErrorCode = "UNAUTHORIZED",
                    TraceId = context.TraceIdentifier
                },
                ForbiddenException ex => new ErrorResponse
                {
                    Message = ex.Message,
                    ErrorCode = "FORBIDDEN",
                    TraceId = context.TraceIdentifier
                },
                _ => new ErrorResponse
                {
                    Message = _environment.IsDevelopment() ? exception.Message : "An internal server error occurred",
                    ErrorCode = "INTERNAL_SERVER_ERROR",
                    TraceId = context.TraceIdentifier,
                    Details = _environment.IsDevelopment() ? new Dictionary<string, object>
                    {
                        ["exception"] = exception.GetType().Name,
                        ["stackTrace"] = exception.StackTrace ?? ""
                    } : null
                }
            };

            response.StatusCode = exception switch
            {
                NotFoundException => (int)HttpStatusCode.NotFound,
                BadRequestException => (int)HttpStatusCode.BadRequest,
                ValidationException => (int)HttpStatusCode.BadRequest,
                ConflictException => (int)HttpStatusCode.Conflict,
                UnauthorizedException => (int)HttpStatusCode.Unauthorized,
                ForbiddenException => (int)HttpStatusCode.Forbidden,
                _ => (int)HttpStatusCode.InternalServerError
            };

            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = _environment.IsDevelopment()
            };

            var jsonResponse = JsonSerializer.Serialize(errorResponse, jsonOptions);
            await response.WriteAsync(jsonResponse);
        }
    }
}