using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZefsjulaApi.Models.Responses;
using ZefsjulaApi.Services;

namespace ZefsjulaApi.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [AllowAnonymous] // Allow public access for analytics
    public class AnalyticsController : ControllerBase
    {
        private readonly ICompanyService _companyService;
        private readonly ILogger<AnalyticsController> _logger;

        public AnalyticsController(ICompanyService companyService, ILogger<AnalyticsController> logger)
        {
            _companyService = companyService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<object>>> GetAnalytics()
        {
            try
            {
                // Get basic analytics data
                var companies = await _companyService.GetAllCompaniesAsync();
                
                if (!companies.Success || companies.Data == null)
                {
                    return Ok(new ApiResponse<object>
                    {
                        Success = true,
                        Message = "No company data available",
                        Data = new
                        {
                            totalCompanies = 0,
                            analytics = "No data available"
                        }
                    });
                }

                var companiesList = companies.Data.ToList();
                
                var analytics = new
                {
                    totalCompanies = companiesList.Count,
                    totalFunding = companiesList.Sum(c => c.FundingTotalUsd ?? 0),
                    averageFunding = companiesList.Where(c => c.FundingTotalUsd.HasValue)
                                                 .Average(c => c.FundingTotalUsd ?? 0),
                    topCountries = companiesList.GroupBy(c => c.CountryCode)
                                               .OrderByDescending(g => g.Count())
                                               .Take(5)
                                               .Select(g => new { country = g.Key, count = g.Count() }),
                    topCategories = companiesList.GroupBy(c => c.CategoryList)
                                                .OrderByDescending(g => g.Count())
                                                .Take(5)
                                                .Select(g => new { category = g.Key, count = g.Count() }),
                    fundingDistribution = new
                    {
                        under1M = companiesList.Count(c => (c.FundingTotalUsd ?? 0) < 1000000),
                        between1M10M = companiesList.Count(c => (c.FundingTotalUsd ?? 0) >= 1000000 && (c.FundingTotalUsd ?? 0) < 10000000),
                        between10M100M = companiesList.Count(c => (c.FundingTotalUsd ?? 0) >= 10000000 && (c.FundingTotalUsd ?? 0) < 100000000),
                        over100M = companiesList.Count(c => (c.FundingTotalUsd ?? 0) >= 100000000)
                    },
                    lastUpdated = DateTime.UtcNow
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

        [HttpGet("summary")]
        public async Task<ActionResult<ApiResponse<object>>> GetSummary()
        {
            try
            {
                var companies = await _companyService.GetAllCompaniesAsync();
                
                if (!companies.Success || companies.Data == null)
                {
                    return Ok(new ApiResponse<object>
                    {
                        Success = true,
                        Message = "No company data available",
                        Data = new { totalCompanies = 0 }
                    });
                }

                var companiesList = companies.Data.ToList();
                
                var summary = new
                {
                    totalCompanies = companiesList.Count,
                    dataStatus = "available",
                    lastUpdated = DateTime.UtcNow,
                    version = "1.0.0"
                };

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Summary retrieved successfully",
                    Data = summary
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve summary data");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Failed to retrieve summary data",
                    Data = null
                });
            }
        }
    }
}
