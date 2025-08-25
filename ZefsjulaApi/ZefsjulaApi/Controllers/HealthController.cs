using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZefsjulaApi.Models.Responses;

namespace ZefsjulaApi.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [AllowAnonymous] // Allow public access for health checks
    public class HealthController : ControllerBase
    {
        private readonly ILogger<HealthController> _logger;

        public HealthController(ILogger<HealthController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public ActionResult<ApiResponse<object>> GetHealth()
        {
            try
            {
                var healthData = new
                {
                    status = "healthy",
                    timestamp = DateTime.UtcNow,
                    version = "1.0.0",
                    environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production",
                    uptime = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC")
                };

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "API is healthy",
                    Data = healthData
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Health check failed");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Health check failed",
                    Data = null
                });
            }
        }
    }
}
