using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZefsjulaApi.Models.Responses;
using ZefsjulaApi.Services;

namespace ZefsjulaApi.Controllers
{
    [ApiController]
    [Route("api/public")]
    [AllowAnonymous] // Allow public access for frontend integration
    [Consumes("application/json")]
    [Produces("application/json")]
    public class PublicApiController : ControllerBase
    {
        private readonly ICompanyService _companyService;
        private readonly ILogger<PublicApiController> _logger;

        public PublicApiController(ICompanyService companyService, ILogger<PublicApiController> logger)
        {
            _companyService = companyService;
            _logger = logger;
        }

        [HttpGet("health")]
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
                    message = "Public API is accessible"
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

        [HttpGet("companies")]
        public async Task<ActionResult<ApiResponse<IEnumerable<object>>>> GetCompaniesForFrontend(
            [FromQuery] int limit = 50)
        {
            try
            {
                var result = await _companyService.GetAllCompaniesAsync();
                
                if (!result.Success || result.Data == null)
                {
                    return Ok(new ApiResponse<IEnumerable<object>>
                    {
                        Success = true,
                        Message = "No company data available",
                        Data = new List<object>()
                    });
                }

                // Return simplified company data for frontend
                var companies = result.Data.Take(limit).Select(c => new
                {
                    id = c.CompanyId,
                    name = c.Name,
                    industry = c.CategoryList,
                    country = c.CountryCode,
                    city = c.City,
                    funding = c.FundingTotalUsd,
                    foundedYear = c.FoundedAt?.Year,
                    status = c.Status
                });

                return Ok(new ApiResponse<IEnumerable<object>>
                {
                    Success = true,
                    Message = $"Retrieved {companies.Count()} companies",
                    Data = companies
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve companies for frontend");
                return StatusCode(500, new ApiResponse<IEnumerable<object>>
                {
                    Success = false,
                    Message = "Failed to retrieve companies",
                    Data = new List<object>()
                });
            }
        }

        [HttpGet("analytics")]
        public async Task<ActionResult<ApiResponse<object>>> GetAnalytics()
        {
            try
            {
                var companies = await _companyService.GetAllCompaniesAsync();
                
                if (!companies.Success || companies.Data == null)
                {
                    return Ok(new ApiResponse<object>
                    {
                        Success = true,
                        Message = "No company data available for analytics",
                        Data = new
                        {
                            totalCompanies = 0,
                            message = "No data available"
                        }
                    });
                }

                var companiesList = companies.Data.ToList();
                
                var analytics = new
                {
                    totalCompanies = companiesList.Count,
                    totalFunding = companiesList.Sum(c => c.FundingTotalUsd ?? 0),
                    averageFunding = companiesList.Where(c => c.FundingTotalUsd.HasValue && c.FundingTotalUsd > 0)
                                                 .DefaultIfEmpty()
                                                 .Average(c => c?.FundingTotalUsd ?? 0),
                    topCountries = companiesList.Where(c => !string.IsNullOrEmpty(c.CountryCode))
                                               .GroupBy(c => c.CountryCode)
                                               .OrderByDescending(g => g.Count())
                                               .Take(5)
                                               .Select(g => new { country = g.Key, count = g.Count() }),
                    topIndustries = companiesList.Where(c => !string.IsNullOrEmpty(c.CategoryList))
                                                .GroupBy(c => c.CategoryList)
                                                .OrderByDescending(g => g.Count())
                                                .Take(5)
                                                .Select(g => new { industry = g.Key, count = g.Count() }),
                    fundingDistribution = new
                    {
                        under1M = companiesList.Count(c => (c.FundingTotalUsd ?? 0) > 0 && (c.FundingTotalUsd ?? 0) < 1000000),
                        between1M10M = companiesList.Count(c => (c.FundingTotalUsd ?? 0) >= 1000000 && (c.FundingTotalUsd ?? 0) < 10000000),
                        between10M100M = companiesList.Count(c => (c.FundingTotalUsd ?? 0) >= 10000000 && (c.FundingTotalUsd ?? 0) < 100000000),
                        over100M = companiesList.Count(c => (c.FundingTotalUsd ?? 0) >= 100000000)
                    },
                    lastUpdated = DateTime.UtcNow,
                    dataSource = ".NET API"
                };

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Analytics data retrieved successfully",
                    Data = analytics
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve analytics data");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Failed to retrieve analytics data",
                    Data = null
                });
            }
        }

        [HttpGet("status")]
        public ActionResult<ApiResponse<object>> GetApiStatus()
        {
            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "Public API is operational",
                Data = new
                {
                    status = "online",
                    timestamp = DateTime.UtcNow,
                    version = "1.0.0",
                    publicEndpoints = new[]
                    {
                        "/api/public/health",
                        "/api/public/companies",
                        "/api/public/analytics",
                        "/api/public/status"
                    }
                }
            });
        }
    }
}
