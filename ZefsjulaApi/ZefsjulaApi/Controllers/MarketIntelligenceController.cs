using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZefsjulaApi.Models.AI;
using ZefsjulaApi.Models.Responses;
using ZefsjulaApi.Services.AI_Interface;

namespace ZefsjulaApi.Controllers
{
    [ApiController]
    [Route("api/v1/market-intelligence")]
    [AllowAnonymous] // For testing purposes
    public class MarketIntelligenceController : ControllerBase
    {
        private readonly IMarketIntelligenceService _marketIntelligenceService;
        private readonly ILogger<MarketIntelligenceController> _logger;

        public MarketIntelligenceController(
            IMarketIntelligenceService marketIntelligenceService,
            ILogger<MarketIntelligenceController> logger)
        {
            _marketIntelligenceService = marketIntelligenceService;
            _logger = logger;
        }

        /// <summary>
        /// 📊 Analyze market segments and discover business opportunities
        /// Automatically groups companies into intelligent market segments with business insights
        /// </summary>
        /// <param name="segments">Number of market segments to identify (2-20, default: 8)</param>
        /// <returns>Comprehensive market intelligence analysis with segments, insights, and opportunities</returns>
        /// <response code="200">Returns market intelligence analysis with segments and insights</response>
        /// <response code="400">Invalid number of segments specified</response>
        /// <response code="500">Internal server error during analysis</response>
        [HttpGet("segments")]
        [ProducesResponseType(typeof(ApiResponse<MarketIntelligenceResponse>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 500)]
        public async Task<ActionResult<ApiResponse<MarketIntelligenceResponse>>> AnalyzeMarketSegments(
            [FromQuery] int segments = 8)
        {
            try
            {
                // Validate input
                if (segments < 2 || segments > 20)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Number of segments must be between 2 and 20",
                        Data = null
                    });
                }

                _logger.LogInformation("🧠 Starting market intelligence analysis with {Segments} segments", segments);

                var result = await _marketIntelligenceService.AnalyzeMarketSegmentsAsync(segments);

                if (result.Success)
                {
                    _logger.LogInformation("✅ Market analysis completed: {Segments} segments identified from {Companies} companies", 
                        result.MarketSegments.Count, result.TotalCompaniesAnalyzed);

                    return Ok(new ApiResponse<MarketIntelligenceResponse>
                    {
                        Success = true,
                        Message = $"Successfully analyzed {result.MarketSegments.Count} market segments from {result.TotalCompaniesAnalyzed} companies",
                        Data = result
                    });
                }
                else
                {
                    _logger.LogError("❌ Market analysis failed: {Message}", result.Message);
                    return StatusCode(500, new ApiResponse<MarketIntelligenceResponse>
                    {
                        Success = false,
                        Message = result.Message,
                        Data = null
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to analyze market segments");
                return StatusCode(500, new ApiResponse<MarketIntelligenceResponse>
                {
                    Success = false,
                    Message = "Failed to analyze market segments. Please try again.",
                    Data = null
                });
            }
        }

        /// <summary>
        /// 🎯 Get high-potential investment opportunities across all market segments
        /// Identifies sectors and companies with strong growth potential and investment appeal
        /// </summary>
        /// <returns>List of high-potential investment opportunities with analysis</returns>
        /// <response code="200">Returns list of investment opportunities</response>
        /// <response code="500">Internal server error during analysis</response>
        [HttpGet("investment-opportunities")]
        [ProducesResponseType(typeof(ApiResponse<List<string>>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 500)]
        public async Task<ActionResult<ApiResponse<List<string>>>> GetInvestmentOpportunities()
        {
            try
            {
                _logger.LogInformation("💰 Analyzing investment opportunities");

                var opportunities = await _marketIntelligenceService.GetInvestmentOpportunitiesAsync();

                _logger.LogInformation("✅ Found {Count} investment opportunities", opportunities.Count);

                return Ok(new ApiResponse<List<string>>
                {
                    Success = true,
                    Message = $"Identified {opportunities.Count} high-potential investment opportunities",
                    Data = opportunities
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to get investment opportunities");
                return StatusCode(500, new ApiResponse<List<string>>
                {
                    Success = false,
                    Message = "Failed to analyze investment opportunities. Please try again.",
                    Data = new List<string>()
                });
            }
        }

        /// <summary>
        /// 📈 Discover emerging trends and growth opportunities in the market
        /// Identifies new business models, technologies, and market shifts before they become mainstream
        /// </summary>
        /// <returns>List of emerging business trends with growth potential</returns>
        /// <response code="200">Returns list of emerging trends</response>
        /// <response code="500">Internal server error during analysis</response>
        [HttpGet("emerging-trends")]
        [ProducesResponseType(typeof(ApiResponse<List<string>>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 500)]
        public async Task<ActionResult<ApiResponse<List<string>>>> GetEmergingTrends()
        {
            try
            {
                _logger.LogInformation("📈 Analyzing emerging trends");

                var trends = await _marketIntelligenceService.GetEmergingTrendsAsync();

                _logger.LogInformation("✅ Identified {Count} emerging trends", trends.Count);

                return Ok(new ApiResponse<List<string>>
                {
                    Success = true,
                    Message = $"Identified {trends.Count} emerging business trends",
                    Data = trends
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to get emerging trends");
                return StatusCode(500, new ApiResponse<List<string>>
                {
                    Success = false,
                    Message = "Failed to analyze emerging trends. Please try again.",
                    Data = new List<string>()
                });
            }
        }

        /// <summary>
        /// 📊 Get comprehensive market sector distribution analysis
        /// Shows how companies are distributed across different market sectors and industries
        /// </summary>
        /// <returns>Distribution of companies across market sectors with counts and percentages</returns>
        /// <response code="200">Returns sector distribution analysis</response>
        /// <response code="500">Internal server error during analysis</response>
        [HttpGet("sector-distribution")]
        [ProducesResponseType(typeof(ApiResponse<Dictionary<string, int>>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 500)]
        public async Task<ActionResult<ApiResponse<Dictionary<string, int>>>> GetSectorDistribution()
        {
            try
            {
                _logger.LogInformation("📊 Analyzing sector distribution");

                var distribution = await _marketIntelligenceService.GetSectorDistributionAsync();

                _logger.LogInformation("✅ Analyzed distribution across {Count} sectors", distribution.Count);

                return Ok(new ApiResponse<Dictionary<string, int>>
                {
                    Success = true,
                    Message = $"Analyzed company distribution across {distribution.Count} market sectors",
                    Data = distribution
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to get sector distribution");
                return StatusCode(500, new ApiResponse<Dictionary<string, int>>
                {
                    Success = false,
                    Message = "Failed to analyze sector distribution. Please try again.",
                    Data = new Dictionary<string, int>()
                });
            }
        }

        /// <summary>
        /// 🔍 Get detailed analysis of a specific market segment
        /// Provides deep insights into a particular market segment including companies, characteristics, and opportunities
        /// </summary>
        /// <param name="segmentId">ID of the market segment to analyze (1-20)</param>
        /// <returns>Detailed segment analysis with companies, trends, and investment insights</returns>
        /// <response code="200">Returns detailed segment analysis</response>
        /// <response code="400">Invalid segment ID</response>
        /// <response code="404">Segment not found</response>
        /// <response code="500">Internal server error during analysis</response>
        [HttpGet("segments/{segmentId}")]
        [ProducesResponseType(typeof(ApiResponse<MarketSegment>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        [ProducesResponseType(typeof(ApiResponse<object>), 500)]
        public async Task<ActionResult<ApiResponse<MarketSegment>>> GetSegmentDetails(int segmentId)
        {
            try
            {
                if (segmentId <= 0)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Segment ID must be a positive number",
                        Data = null
                    });
                }

                _logger.LogInformation("🔍 Getting details for segment {SegmentId}", segmentId);

                var segment = await _marketIntelligenceService.GetSegmentDetailsAsync(segmentId);

                if (segment == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"Market segment with ID {segmentId} not found",
                        Data = null
                    });
                }

                _logger.LogInformation("✅ Retrieved details for segment {SegmentId}: {SegmentName}", 
                    segmentId, segment.SegmentName);

                return Ok(new ApiResponse<MarketSegment>
                {
                    Success = true,
                    Message = $"Retrieved detailed analysis for market segment: {segment.SegmentName}",
                    Data = segment
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to get segment details for {SegmentId}", segmentId);
                return StatusCode(500, new ApiResponse<MarketSegment>
                {
                    Success = false,
                    Message = "Failed to get segment details. Please try again.",
                    Data = null
                });
            }
        }

        /// <summary>
        /// 🚀 Get quick market overview with key insights
        /// Provides a summary of the most important market intelligence insights in one call
        /// </summary>
        /// <returns>Quick market overview with top insights, trends, and opportunities</returns>
        /// <response code="200">Returns market overview</response>
        /// <response code="500">Internal server error during analysis</response>
        [HttpGet("overview")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 500)]
        public async Task<ActionResult<ApiResponse<object>>> GetMarketOverview()
        {
            try
            {
                _logger.LogInformation("🚀 Generating market overview");

                // Get market segments with fewer clusters for overview
                var marketAnalysis = await _marketIntelligenceService.AnalyzeMarketSegmentsAsync(5);
                
                if (!marketAnalysis.Success)
                {
                    return StatusCode(500, new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Failed to generate market overview",
                        Data = null
                    });
                }

                var overview = new
                {
                    Summary = new
                    {
                        TotalCompanies = marketAnalysis.TotalCompaniesAnalyzed,
                        MarketSegments = marketAnalysis.MarketSegments.Count,
                        AnalysisDate = marketAnalysis.AnalysisDate
                    },
                    TopSegments = marketAnalysis.MarketSegments
                        .OrderByDescending(s => s.AverageScore)
                        .Take(3)
                        .Select(s => new
                        {
                            s.SegmentName,
                            s.CompanyCount,
                            s.InvestmentOpportunity,
                            s.GrowthTrend,
                            Score = Math.Round(s.AverageScore, 1)
                        }),
                    KeyInsights = new
                    {
                        marketAnalysis.GlobalInsights.HottestSector,
                        marketAnalysis.GlobalInsights.EmergingTrend,
                        TopOpportunities = marketAnalysis.GlobalInsights.InvestmentOpportunities.Take(3),
                        MarketGaps = marketAnalysis.GlobalInsights.MarketGaps.Take(3)
                    },
                    QuickStats = new
                    {
                        HighGrowthSegments = marketAnalysis.MarketSegments.Count(s => s.GrowthTrend == "High Growth"),
                        HighPotentialSegments = marketAnalysis.MarketSegments.Count(s => s.InvestmentOpportunity == "High Potential"),
                        AverageSegmentSize = marketAnalysis.MarketSegments.Count > 0 
                            ? Math.Round(marketAnalysis.MarketSegments.Average(s => s.CompanyCount), 1) 
                            : 0
                    }
                };

                _logger.LogInformation("✅ Market overview generated successfully");

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Market overview generated successfully",
                    Data = overview
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to generate market overview");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Failed to generate market overview. Please try again.",
                    Data = null
                });
            }
        }

        /// <summary>
        /// 💡 Get actionable business insights and recommendations
        /// Provides specific, actionable recommendations based on market analysis
        /// </summary>
        /// <returns>Business insights and strategic recommendations</returns>
        /// <response code="200">Returns business insights and recommendations</response>
        /// <response code="500">Internal server error during analysis</response>
        [HttpGet("insights")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 500)]
        public async Task<ActionResult<ApiResponse<object>>> GetBusinessInsights()
        {
            try
            {
                _logger.LogInformation("💡 Generating business insights");

                var marketAnalysis = await _marketIntelligenceService.AnalyzeMarketSegmentsAsync(6);
                
                if (!marketAnalysis.Success)
                {
                    return StatusCode(500, new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Failed to generate business insights",
                        Data = null
                    });
                }

                var insights = new
                {
                    ForInvestors = new
                    {
                        TopInvestmentTargets = marketAnalysis.MarketSegments
                            .Where(s => s.InvestmentOpportunity == "High Potential")
                            .OrderByDescending(s => s.AverageScore)
                            .Take(3)
                            .Select(s => new { s.SegmentName, s.CompanyCount, Score = Math.Round(s.AverageScore, 1) }),
                        
                        RiskDiversification = marketAnalysis.MarketSegments
                            .Where(s => s.GrowthTrend == "Steady Growth" && s.CompanyCount >= 10)
                            .Take(2)
                            .Select(s => s.SegmentName),
                        
                        EmergingBets = marketAnalysis.MarketSegments
                            .Where(s => s.GrowthTrend == "High Growth" && s.CompanyCount < 10)
                            .Take(2)
                            .Select(s => s.SegmentName)
                    },
                    
                    ForEntrepreneurs = new
                    {
                        MarketGaps = marketAnalysis.GlobalInsights.MarketGaps.Take(3),
                        UndersaturatedMarkets = marketAnalysis.MarketSegments
                            .Where(s => s.CompanyCount < 5 && s.AverageScore > 50)
                            .Select(s => s.SegmentName)
                            .Take(3),
                        GrowthOpportunities = marketAnalysis.MarketSegments
                            .Where(s => s.GrowthTrend == "High Growth")
                            .Select(s => s.SegmentName)
                            .Take(3)
                    },
                    
                    ForAnalysts = new
                    {
                        MarketConcentration = new
                        {
                            TopSegmentShare = marketAnalysis.MarketSegments.Count > 0 
                                ? Math.Round((double)marketAnalysis.MarketSegments.Max(s => s.CompanyCount) / marketAnalysis.TotalCompaniesAnalyzed * 100, 1)
                                : 0,
                            FragmentationIndex = marketAnalysis.MarketSegments.Count > 0 
                                ? Math.Round(1.0 / marketAnalysis.MarketSegments.Count, 3)
                                : 0
                        },
                        TrendStrength = marketAnalysis.MarketSegments
                            .GroupBy(s => s.GrowthTrend)
                            .ToDictionary(g => g.Key, g => g.Count()),
                        InnovationIndex = marketAnalysis.MarketSegments
                            .Where(s => s.KeyCharacteristics.Any(k => k.Contains("AI") || k.Contains("Blockchain") || k.Contains("Quantum")))
                            .Count()
                    },
                    
                    StrategicRecommendations = GenerateStrategicRecommendations(marketAnalysis.MarketSegments, marketAnalysis.GlobalInsights)
                };

                _logger.LogInformation("✅ Business insights generated successfully");

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Business insights and recommendations generated successfully",
                    Data = insights
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to generate business insights");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Failed to generate business insights. Please try again.",
                    Data = null
                });
            }
        }

        private List<string> GenerateStrategicRecommendations(List<MarketSegment> segments, MarketInsights insights)
        {
            var recommendations = new List<string>();

            // Investment recommendations
            var highPotentialCount = segments.Count(s => s.InvestmentOpportunity == "High Potential");
            if (highPotentialCount > 0)
            {
                recommendations.Add($"Focus investment efforts on {highPotentialCount} high-potential segments, particularly {insights.HottestSector}");
            }

            // Market gap recommendations
            if (insights.MarketGaps.Any())
            {
                recommendations.Add($"Explore untapped opportunities in {string.Join(", ", insights.MarketGaps.Take(2))} - these markets show potential but low competition");
            }

            // Diversification recommendations
            var stableSegments = segments.Count(s => s.GrowthTrend == "Steady Growth");
            if (stableSegments >= 2)
            {
                recommendations.Add($"Balance portfolio with {stableSegments} stable growth segments for risk management");
            }

            // Emerging trend recommendations
            if (!string.IsNullOrEmpty(insights.EmergingTrend))
            {
                recommendations.Add($"Monitor {insights.EmergingTrend} closely - this emerging trend could disrupt traditional markets");
            }

            // Innovation recommendations
            var innovationSegments = segments.Where(s => 
                s.KeyCharacteristics.Any(k => k.Contains("AI") || k.Contains("Machine Learning") || k.Contains("Blockchain")))
                .Count();
            
            if (innovationSegments > 0)
            {
                recommendations.Add($"Prioritize {innovationSegments} innovation-driven segments for long-term competitive advantage");
            }

            return recommendations.Take(5).ToList();
        }
    }
}
