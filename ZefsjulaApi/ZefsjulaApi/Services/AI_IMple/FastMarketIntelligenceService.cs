using Microsoft.Extensions.Caching.Memory;
using ZefsjulaApi.Models.AI;
using ZefsjulaApi.Services.AI_Interface;
using ZefsjulaApi.Services;
using ZefsjulaApi.Models.DTO;

namespace ZefsjulaApi.Services.AI_IMple
{
    public class FastMarketIntelligenceService : IMarketIntelligenceService
    {
        private readonly ICompanyService _companyService;
        private readonly ILogger<FastMarketIntelligenceService> _logger;
        private readonly IMemoryCache _cache;

        public FastMarketIntelligenceService(
            ICompanyService companyService,
            ILogger<FastMarketIntelligenceService> logger,
            IMemoryCache cache)
        {
            _companyService = companyService;
            _logger = logger;
            _cache = cache;
        }

        public async Task<MarketIntelligenceResponse> AnalyzeMarketSegmentsAsync(int numberOfSegments = 8)
        {
            try
            {
                var cacheKey = $"market_segments_{numberOfSegments}";
                
                // Try to get from cache first (5 minute cache)
                if (_cache.TryGetValue(cacheKey, out MarketIntelligenceResponse cachedResult))
                {
                    _logger.LogInformation("✅ Returning cached market intelligence analysis");
                    return cachedResult;
                }

                _logger.LogInformation("🧠 Starting fast market intelligence analysis with {Segments} segments", numberOfSegments);

                // Get companies quickly (limit to 100 for speed)
                var companies = await GetCompaniesQuickAsync(100);
                
                if (companies.Count < numberOfSegments)
                {
                    numberOfSegments = Math.Max(2, companies.Count / 2);
                }

                // Create market segments using fast rule-based approach
                var marketSegments = await CreateSegmentsQuickAsync(companies, numberOfSegments);

                // Generate insights
                var globalInsights = GenerateQuickInsights(marketSegments);

                var response = new MarketIntelligenceResponse
                {
                    Success = true,
                    Message = $"Successfully analyzed {marketSegments.Count} market segments",
                    MarketSegments = marketSegments,
                    GlobalInsights = globalInsights,
                    TotalCompaniesAnalyzed = companies.Count
                };

                // Cache for 5 minutes
                _cache.Set(cacheKey, response, TimeSpan.FromMinutes(5));

                _logger.LogInformation("✅ Fast market intelligence analysis completed: {Segments} segments, {Companies} companies", 
                    marketSegments.Count, companies.Count);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to analyze market segments");
                return new MarketIntelligenceResponse
                {
                    Success = false,
                    Message = $"Failed to analyze market segments: {ex.Message}"
                };
            }
        }

        private async Task<List<CompanyDto>> GetCompaniesQuickAsync(int limit = 100)
        {
            var companiesResponse = await _companyService.GetAllCompaniesAsync();
            return companiesResponse.Data?.Take(limit).ToList() ?? new List<CompanyDto>();
        }

        private async Task<List<MarketSegment>> CreateSegmentsQuickAsync(List<CompanyDto> companies, int numberOfSegments)
        {
            var segments = new List<MarketSegment>();
            
            // Pre-defined sector keywords for fast categorization
            var sectorKeywords = new Dictionary<string, List<string>>
            {
                { "AI & Machine Learning", new[] { "ai", "artificial", "machine learning", "ml", "neural", "deep learning" }.ToList() },
                { "Financial Technology", new[] { "fintech", "finance", "banking", "payment", "crypto", "blockchain" }.ToList() },
                { "Healthcare & Biotechnology", new[] { "health", "medical", "bio", "pharma", "healthcare", "medicine" }.ToList() },
                { "E-commerce & Retail", new[] { "ecommerce", "retail", "shopping", "marketplace", "commerce" }.ToList() },
                { "Energy & Sustainability", new[] { "energy", "solar", "renewable", "green", "sustainable", "clean" }.ToList() },
                { "Software & Cloud", new[] { "software", "cloud", "saas", "platform", "app", "digital" }.ToList() },
                { "Transportation & Mobility", new[] { "transport", "mobility", "automotive", "logistics", "delivery" }.ToList() },
                { "Media & Entertainment", new[] { "media", "entertainment", "gaming", "content", "streaming" }.ToList() }
            };

            // Categorize companies into segments
            var companySegments = new Dictionary<string, List<CompanyDto>>();
            var uncategorized = new List<CompanyDto>();

            foreach (var company in companies)
            {
                var companyText = $"{company.Name} {company.CategoryList} {company.Status}".ToLower();
                var assigned = false;

                foreach (var sector in sectorKeywords)
                {
                    if (sector.Value.Any(keyword => companyText.Contains(keyword)))
                    {
                        if (!companySegments.ContainsKey(sector.Key))
                            companySegments[sector.Key] = new List<CompanyDto>();
                        
                        companySegments[sector.Key].Add(company);
                        assigned = true;
                        break;
                    }
                }

                if (!assigned)
                {
                    uncategorized.Add(company);
                }
            }

            // Create market segments
            var segmentId = 1;
            foreach (var segment in companySegments.Where(s => s.Value.Count > 0).Take(numberOfSegments))
            {
                var companies_in_segment = segment.Value;
                var characteristics = GetSegmentCharacteristics(segment.Key);

                var marketSegment = new MarketSegment
                {
                    SegmentId = segmentId++,
                    SegmentName = segment.Key,
                    Description = $"Market segment focused on {segment.Key.ToLower()} with {companies_in_segment.Count} companies.",
                    CompanyCount = companies_in_segment.Count,
                    Companies = companies_in_segment.Select(c => new CompanyEmbedding 
                    { 
                        CompanyId = c.CompanyId, 
                        CompanyName = c.Name ?? "Unknown",
                        ClusterId = segmentId - 1
                    }).ToList(),
                    KeyCharacteristics = characteristics,
                    InvestmentOpportunity = DetermineInvestmentOpportunityQuick(companies_in_segment.Count, segment.Key),
                    GrowthTrend = DetermineGrowthTrendQuick(segment.Key),
                    AverageScore = CalculateSegmentScoreQuick(segment.Key, companies_in_segment.Count),
                    Analytics = new Dictionary<string, object>
                    {
                        { "CompanyCount", companies_in_segment.Count },
                        { "SectorType", segment.Key },
                        { "MarketMaturity", companies_in_segment.Count > 15 ? "Mature" : "Emerging" }
                    }
                };

                segments.Add(marketSegment);
            }

            // Add uncategorized as "General Technology" if we have room
            if (uncategorized.Count > 0 && segments.Count < numberOfSegments)
            {
                segments.Add(new MarketSegment
                {
                    SegmentId = segmentId,
                    SegmentName = "General Technology",
                    Description = $"General technology companies with {uncategorized.Count} companies.",
                    CompanyCount = uncategorized.Count,
                    Companies = uncategorized.Select(c => new CompanyEmbedding 
                    { 
                        CompanyId = c.CompanyId, 
                        CompanyName = c.Name ?? "Unknown",
                        ClusterId = segmentId
                    }).ToList(),
                    KeyCharacteristics = new List<string> { "Technology", "Software", "Innovation" },
                    InvestmentOpportunity = "Emerging Opportunity",
                    GrowthTrend = "Steady Growth",
                    AverageScore = 60.0,
                    Analytics = new Dictionary<string, object>
                    {
                        { "CompanyCount", uncategorized.Count },
                        { "SectorType", "Mixed Technology" }
                    }
                });
            }

            return segments.OrderByDescending(s => s.CompanyCount).ToList();
        }

        private List<string> GetSegmentCharacteristics(string segmentName)
        {
            var characteristics = new Dictionary<string, List<string>>
            {
                { "AI & Machine Learning", new[] { "Artificial Intelligence", "Machine Learning", "Neural Networks", "Automation", "Computer Vision" }.ToList() },
                { "Financial Technology", new[] { "Digital Payments", "Banking", "Cryptocurrency", "Investment", "Financial Services" }.ToList() },
                { "Healthcare & Biotechnology", new[] { "Digital Health", "Medical Technology", "Biotechnology", "Pharmaceuticals", "Healthcare Services" }.ToList() },
                { "E-commerce & Retail", new[] { "Online Shopping", "Retail Technology", "Marketplace", "Consumer Goods", "Digital Commerce" }.ToList() },
                { "Energy & Sustainability", new[] { "Renewable Energy", "Clean Technology", "Sustainability", "Green Energy", "Environmental" }.ToList() },
                { "Software & Cloud", new[] { "Cloud Computing", "Software Development", "SaaS", "Enterprise Software", "Digital Platforms" }.ToList() },
                { "Transportation & Mobility", new[] { "Transportation", "Logistics", "Mobility Solutions", "Automotive", "Supply Chain" }.ToList() },
                { "Media & Entertainment", new[] { "Digital Media", "Entertainment", "Gaming", "Content Creation", "Streaming" }.ToList() }
            };

            return characteristics.GetValueOrDefault(segmentName, new List<string> { "Technology", "Innovation" });
        }

        private string DetermineInvestmentOpportunityQuick(int companyCount, string sectorName)
        {
            var hotSectors = new[] { "AI & Machine Learning", "Financial Technology", "Healthcare & Biotechnology", "Energy & Sustainability" };
            
            if (hotSectors.Contains(sectorName) && companyCount >= 8)
                return "High Potential";
            else if (hotSectors.Contains(sectorName) || companyCount >= 12)
                return "Moderate Potential";
            else
                return "Emerging Opportunity";
        }

        private string DetermineGrowthTrendQuick(string sectorName)
        {
            var highGrowthSectors = new[] { "AI & Machine Learning", "Energy & Sustainability", "Healthcare & Biotechnology" };
            var steadyGrowthSectors = new[] { "Software & Cloud", "E-commerce & Retail", "Financial Technology" };

            if (highGrowthSectors.Contains(sectorName))
                return "High Growth";
            else if (steadyGrowthSectors.Contains(sectorName))
                return "Steady Growth";
            else
                return "Emerging";
        }

        private double CalculateSegmentScoreQuick(string sectorName, int companyCount)
        {
            var baseScore = Math.Min(companyCount * 3, 60); // Base score from company count
            
            var sectorBonuses = new Dictionary<string, int>
            {
                { "AI & Machine Learning", 25 },
                { "Financial Technology", 20 },
                { "Healthcare & Biotechnology", 22 },
                { "Energy & Sustainability", 24 },
                { "Software & Cloud", 15 },
                { "E-commerce & Retail", 12 },
                { "Transportation & Mobility", 18 },
                { "Media & Entertainment", 10 }
            };

            var sectorBonus = sectorBonuses.GetValueOrDefault(sectorName, 10);
            
            return Math.Min(baseScore + sectorBonus, 100);
        }

        private MarketInsights GenerateQuickInsights(List<MarketSegment> segments)
        {
            return new MarketInsights
            {
                HottestSector = segments.OrderByDescending(s => s.AverageScore).FirstOrDefault()?.SegmentName ?? "AI & Machine Learning",
                EmergingTrend = segments.Where(s => s.GrowthTrend == "High Growth").OrderBy(s => s.CompanyCount).FirstOrDefault()?.SegmentName ?? "Energy & Sustainability",
                InvestmentOpportunities = segments.Where(s => s.InvestmentOpportunity == "High Potential").Select(s => s.SegmentName).Take(3).ToList(),
                MarketGaps = segments.Where(s => s.CompanyCount < 5).Select(s => s.SegmentName).Take(3).ToList(),
                SectorDistribution = segments.ToDictionary(s => s.SegmentName, s => s.CompanyCount)
            };
        }

        // Implement other methods with caching and quick responses
        public async Task<MarketSegment> GetSegmentDetailsAsync(int segmentId)
        {
            var cacheKey = $"segment_details_{segmentId}";
            
            if (_cache.TryGetValue(cacheKey, out MarketSegment cachedSegment))
            {
                return cachedSegment;
            }

            var marketAnalysis = await AnalyzeMarketSegmentsAsync(10);
            var segment = marketAnalysis.MarketSegments.FirstOrDefault(s => s.SegmentId == segmentId);
            
            if (segment != null)
            {
                _cache.Set(cacheKey, segment, TimeSpan.FromMinutes(10));
            }

            return segment;
        }

        public async Task<List<string>> GetInvestmentOpportunitiesAsync()
        {
            var cacheKey = "investment_opportunities";
            
            if (_cache.TryGetValue(cacheKey, out List<string> cachedOpportunities))
            {
                return cachedOpportunities;
            }

            var opportunities = new List<string>
            {
                "AI & Machine Learning - High growth potential with emerging applications",
                "Energy & Sustainability - Strong government support and market demand",
                "Healthcare & Biotechnology - Aging population driving innovation",
                "Financial Technology - Digital transformation accelerating",
                "Quantum Computing - Early stage but revolutionary potential"
            };

            _cache.Set(cacheKey, opportunities, TimeSpan.FromMinutes(15));
            return opportunities;
        }

        public async Task<List<string>> GetEmergingTrendsAsync()
        {
            var cacheKey = "emerging_trends";
            
            if (_cache.TryGetValue(cacheKey, out List<string> cachedTrends))
            {
                return cachedTrends;
            }

            var trends = new List<string>
            {
                "Generative AI Applications",
                "Sustainable Technology Solutions",
                "Digital Health Platforms",
                "Web3 & Decentralized Systems",
                "Autonomous Vehicle Technology",
                "Clean Energy Storage",
                "Quantum Computing",
                "Extended Reality (AR/VR/MR)"
            };

            _cache.Set(cacheKey, trends, TimeSpan.FromMinutes(15));
            return trends;
        }

        public async Task<Dictionary<string, int>> GetSectorDistributionAsync()
        {
            var cacheKey = "sector_distribution";
            
            if (_cache.TryGetValue(cacheKey, out Dictionary<string, int> cachedDistribution))
            {
                return cachedDistribution;
            }

            var marketAnalysis = await AnalyzeMarketSegmentsAsync(8);
            var distribution = marketAnalysis.GlobalInsights.SectorDistribution;

            _cache.Set(cacheKey, distribution, TimeSpan.FromMinutes(10));
            return distribution;
        }
    }
}
