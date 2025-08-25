using Microsoft.ML;
using Microsoft.ML.Data;
using ZefsjulaApi.Models.AI;
using ZefsjulaApi.Services.AI_Interface;
using ZefsjulaApi.Services;
using ZefsjulaApi.Models.DTO;

namespace ZefsjulaApi.Services.AI_IMple
{
    public class MarketIntelligenceService : IMarketIntelligenceService
    {
        private readonly IEmbeddingService _embeddingService;
        private readonly ICompanyService _companyService;
        private readonly ILogger<MarketIntelligenceService> _logger;
        private readonly MLContext _mlContext;

        public MarketIntelligenceService(
            IEmbeddingService embeddingService,
            ICompanyService companyService,
            ILogger<MarketIntelligenceService> logger)
        {
            _embeddingService = embeddingService;
            _companyService = companyService;
            _logger = logger;
            _mlContext = new MLContext(seed: 0);
        }

        public async Task<MarketIntelligenceResponse> AnalyzeMarketSegmentsAsync(int numberOfSegments = 8)
        {
            try
            {
                _logger.LogInformation("🧠 Starting market intelligence analysis with {Segments} segments", numberOfSegments);

                // 1. Get all companies and generate embeddings
                var companies = await GetCompaniesWithEmbeddingsAsync();
                
                if (companies.Count < numberOfSegments)
                {
                    numberOfSegments = Math.Max(2, companies.Count / 2);
                    _logger.LogWarning("Adjusted segments to {Segments} based on company count", numberOfSegments);
                }

                // 2. Perform K-means clustering
                var clusters = await PerformKMeansClusteringAsync(companies, numberOfSegments);

                // 3. Analyze each cluster to create market segments
                var marketSegments = await AnalyzeClustersAsync(clusters);

                // 4. Generate global market insights
                var globalInsights = GenerateGlobalInsights(marketSegments);

                var response = new MarketIntelligenceResponse
                {
                    Success = true,
                    Message = $"Successfully analyzed {marketSegments.Count} market segments",
                    MarketSegments = marketSegments,
                    GlobalInsights = globalInsights,
                    TotalCompaniesAnalyzed = companies.Count
                };

                _logger.LogInformation("✅ Market intelligence analysis completed: {Segments} segments, {Companies} companies", 
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

        private async Task<List<CompanyEmbedding>> GetCompaniesWithEmbeddingsAsync()
        {
            var companiesResponse = await _companyService.GetAllCompaniesAsync();
            var companies = companiesResponse.Data?.Take(500).ToList() ?? new List<CompanyDto>(); // Limit for performance

            var companyEmbeddings = new List<CompanyEmbedding>();

            foreach (var company in companies)
            {
                try
                {
                    var text = $"{company.Name} {company.CategoryList} {company.Status} {company.City} {company.CountryCode}".Trim();
                    var embedding = await _embeddingService.GenerateEmbeddingAsync(text);

                    if (embedding.Length > 0)
                    {
                        companyEmbeddings.Add(new CompanyEmbedding
                        {
                            CompanyId = company.CompanyId,
                            CompanyName = company.Name ?? "Unknown",
                            Embedding = embedding
                        });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Failed to generate embedding for company {CompanyId}: {Error}", 
                        company.CompanyId, ex.Message);
                }
            }

            return companyEmbeddings;
        }

        private async Task<List<CompanyCluster>> PerformKMeansClusteringAsync(
            List<CompanyEmbedding> companies, int numberOfClusters)
        {
            if (companies.Count == 0)
                return new List<CompanyCluster>();

            try
            {
                // Prepare data for ML.NET
                var embeddingSize = companies.First().Embedding.Length;
                var clusteringData = companies.Select(c => new ClusteringInputData
                {
                    Features = c.Embedding
                }).ToArray();

                var dataView = _mlContext.Data.LoadFromEnumerable(clusteringData);

                // Create K-means clustering pipeline
                var pipeline = _mlContext.Clustering.Trainers.KMeans(
                    featureColumnName: nameof(ClusteringInputData.Features),
                    numberOfClusters: numberOfClusters);

                // Train the model
                var model = pipeline.Fit(dataView);

                // Make predictions
                var predictions = model.Transform(dataView);
                var clusterResults = _mlContext.Data.CreateEnumerable<ClusteringPrediction>(predictions, reuseRowObject: false).ToArray();

                // Group companies by cluster
                var clusters = new List<CompanyCluster>();
                for (int i = 0; i < numberOfClusters; i++)
                {
                    var cluster = new CompanyCluster
                    {
                        ClusterId = i,
                        ClusterName = $"Cluster {i + 1}",
                        Companies = new List<CompanyEmbedding>()
                    };
                    clusters.Add(cluster);
                }

                // Assign companies to clusters
                for (int i = 0; i < companies.Count && i < clusterResults.Length; i++)
                {
                    var clusterId = (int)clusterResults[i].PredictedClusterId;
                    if (clusterId >= 0 && clusterId < clusters.Count)
                    {
                        companies[i].ClusterId = clusterId;
                        clusters[clusterId].Companies.Add(companies[i]);
                    }
                }

                _logger.LogInformation("✅ K-means clustering completed: {Clusters} clusters created", clusters.Count);
                return clusters;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ K-means clustering failed");
                throw;
            }
        }

        private async Task<List<MarketSegment>> AnalyzeClustersAsync(List<CompanyCluster> clusters)
        {
            var marketSegments = new List<MarketSegment>();
            var segmentId = 1;

            foreach (var cluster in clusters.Where(c => c.Companies.Count > 0))
            {
                var segment = new MarketSegment
                {
                    SegmentId = segmentId++,
                    CompanyCount = cluster.Companies.Count,
                    Companies = cluster.Companies
                };

                // Analyze company names and categories to determine segment characteristics
                var companyNames = cluster.Companies.Select(c => c.CompanyName.ToLower()).ToList();
                var characteristics = await AnalyzeSegmentCharacteristicsAsync(companyNames);

                segment.SegmentName = GenerateSegmentName(characteristics);
                segment.Description = GenerateSegmentDescription(characteristics, segment.CompanyCount);
                segment.KeyCharacteristics = characteristics.Take(5).ToList();
                segment.InvestmentOpportunity = DetermineInvestmentOpportunity(segment.CompanyCount, characteristics);
                segment.GrowthTrend = DetermineGrowthTrend(characteristics);
                segment.AverageScore = CalculateSegmentScore(characteristics, segment.CompanyCount);
                segment.Analytics = GenerateSegmentAnalytics(cluster.Companies, characteristics);

                marketSegments.Add(segment);
            }

            return marketSegments.OrderByDescending(s => s.CompanyCount).ToList();
        }

        private async Task<List<string>> AnalyzeSegmentCharacteristicsAsync(List<string> companyNames)
        {
            var characteristics = new List<string>();

            // Common technology keywords
            var techKeywords = new Dictionary<string, string>
            {
                { "ai", "Artificial Intelligence" },
                { "machine learning", "Machine Learning" },
                { "fintech", "Financial Technology" },
                { "blockchain", "Blockchain" },
                { "crypto", "Cryptocurrency" },
                { "health", "Healthcare" },
                { "medical", "Healthcare" },
                { "bio", "Biotechnology" },
                { "energy", "Energy" },
                { "solar", "Renewable Energy" },
                { "ecommerce", "E-commerce" },
                { "retail", "Retail" },
                { "logistics", "Logistics" },
                { "transport", "Transportation" },
                { "education", "Education" },
                { "gaming", "Gaming" },
                { "media", "Media" },
                { "software", "Software" },
                { "cloud", "Cloud Computing" },
                { "security", "Cybersecurity" }
            };

            var keywordCounts = new Dictionary<string, int>();

            foreach (var companyName in companyNames)
            {
                foreach (var keyword in techKeywords.Keys)
                {
                    if (companyName.Contains(keyword))
                    {
                        var characteristic = techKeywords[keyword];
                        keywordCounts[characteristic] = keywordCounts.GetValueOrDefault(characteristic, 0) + 1;
                    }
                }
            }

            // Return top characteristics
            return keywordCounts
                .OrderByDescending(kv => kv.Value)
                .Take(10)
                .Select(kv => kv.Key)
                .ToList();
        }

        private string GenerateSegmentName(List<string> characteristics)
        {
            if (characteristics.Count == 0)
                return "General Technology";

            if (characteristics.Count == 1)
                return characteristics.First();

            // Combine top 2 characteristics
            return characteristics.Take(2).Aggregate((a, b) => $"{a} & {b}");
        }

        private string GenerateSegmentDescription(List<string> characteristics, int companyCount)
        {
            var primaryCharacteristic = characteristics.FirstOrDefault() ?? "technology";
            return $"Market segment focused on {primaryCharacteristic.ToLower()} with {companyCount} companies. " +
                   $"Key areas include: {string.Join(", ", characteristics.Take(3).Select(c => c.ToLower()))}.";
        }

        private string DetermineInvestmentOpportunity(int companyCount, List<string> characteristics)
        {
            var hotSectors = new[] { "Artificial Intelligence", "Machine Learning", "Blockchain", "Cybersecurity", "Biotechnology" };
            var hasHotSector = characteristics.Any(c => hotSectors.Contains(c));

            if (hasHotSector && companyCount >= 10)
                return "High Potential";
            else if (hasHotSector || companyCount >= 15)
                return "Moderate Potential";
            else
                return "Emerging Opportunity";
        }

        private string DetermineGrowthTrend(List<string> characteristics)
        {
            var highGrowthSectors = new[] { "Artificial Intelligence", "Machine Learning", "Blockchain", "Renewable Energy" };
            var matureGrowthSectors = new[] { "E-commerce", "Software", "Cloud Computing" };

            if (characteristics.Any(c => highGrowthSectors.Contains(c)))
                return "High Growth";
            else if (characteristics.Any(c => matureGrowthSectors.Contains(c)))
                return "Steady Growth";
            else
                return "Emerging";
        }

        private double CalculateSegmentScore(List<string> characteristics, int companyCount)
        {
            var baseScore = Math.Min(companyCount * 2, 50); // Max 50 points for company count
            
            var hotSectorBonus = characteristics.Count(c => 
                new[] { "Artificial Intelligence", "Machine Learning", "Blockchain" }.Contains(c)) * 10;

            var diversityBonus = Math.Min(characteristics.Count * 2, 20); // Max 20 points for diversity

            return Math.Min(baseScore + hotSectorBonus + diversityBonus, 100);
        }

        private Dictionary<string, object> GenerateSegmentAnalytics(List<CompanyEmbedding> companies, List<string> characteristics)
        {
            return new Dictionary<string, object>
            {
                { "CompanyCount", companies.Count },
                { "TopCharacteristics", characteristics.Take(3) },
                { "DiversityIndex", Math.Min(characteristics.Count / 5.0, 1.0) },
                { "ConcentrationRatio", companies.Count > 0 ? 1.0 / companies.Count : 0 }
            };
        }

        private MarketInsights GenerateGlobalInsights(List<MarketSegment> segments)
        {
            var insights = new MarketInsights();

            // Find hottest sector (highest scoring segment)
            var hottestSegment = segments.OrderByDescending(s => s.AverageScore).FirstOrDefault();
            insights.HottestSector = hottestSegment?.SegmentName ?? "Technology";

            // Identify emerging trend (smallest segment with high growth)
            var emergingSegment = segments
                .Where(s => s.GrowthTrend == "High Growth")
                .OrderBy(s => s.CompanyCount)
                .FirstOrDefault();
            insights.EmergingTrend = emergingSegment?.SegmentName ?? "AI & Machine Learning";

            // Investment opportunities
            insights.InvestmentOpportunities = segments
                .Where(s => s.InvestmentOpportunity == "High Potential")
                .Select(s => s.SegmentName)
                .Take(3)
                .ToList();

            // Market gaps (segments with few companies but high potential)
            insights.MarketGaps = segments
                .Where(s => s.CompanyCount < 5 && s.AverageScore > 60)
                .Select(s => s.SegmentName)
                .Take(3)
                .ToList();

            // Sector distribution
            insights.SectorDistribution = segments.ToDictionary(s => s.SegmentName, s => s.CompanyCount);

            return insights;
        }

        public async Task<MarketSegment> GetSegmentDetailsAsync(int segmentId)
        {
            try
            {
                _logger.LogInformation("🔍 Getting segment details for segment {SegmentId}", segmentId);

                // Get market segments first
                var marketAnalysis = await AnalyzeMarketSegmentsAsync(10);
                
                if (!marketAnalysis.Success || !marketAnalysis.MarketSegments.Any())
                {
                    return null;
                }

                // Find the requested segment
                var segment = marketAnalysis.MarketSegments.FirstOrDefault(s => s.SegmentId == segmentId);
                
                if (segment == null)
                {
                    _logger.LogWarning("Segment {SegmentId} not found", segmentId);
                    return null;
                }

                // Enhance the segment with additional details
                segment.Analytics["DetailedAnalysis"] = new
                {
                    CompanyDensity = segment.CompanyCount > 0 ? Math.Round(segment.AverageScore / segment.CompanyCount, 2) : 0,
                    MarketPosition = segment.AverageScore > 80 ? "Leader" : segment.AverageScore > 60 ? "Challenger" : "Emerging",
                    CompetitiveIntensity = segment.CompanyCount > 20 ? "High" : segment.CompanyCount > 10 ? "Medium" : "Low",
                    InvestmentReadiness = segment.InvestmentOpportunity == "High Potential" ? "Ready" : "Developing"
                };

                _logger.LogInformation("✅ Retrieved details for segment: {SegmentName}", segment.SegmentName);
                return segment;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to get segment details for {SegmentId}", segmentId);
                return null;
            }
        }

        public async Task<List<string>> GetInvestmentOpportunitiesAsync()
        {
            try
            {
                _logger.LogInformation("💰 Analyzing investment opportunities");

                // Get market segments
                var marketAnalysis = await AnalyzeMarketSegmentsAsync(8);
                
                if (!marketAnalysis.Success)
                {
                    return new List<string>();
                }

                var opportunities = new List<string>();

                // High-potential segments
                var highPotentialSegments = marketAnalysis.MarketSegments
                    .Where(s => s.InvestmentOpportunity == "High Potential")
                    .OrderByDescending(s => s.AverageScore)
                    .Take(3)
                    .Select(s => $"{s.SegmentName} - {s.CompanyCount} companies, Score: {s.AverageScore:F1}")
                    .ToList();

                opportunities.AddRange(highPotentialSegments);

                // Emerging high-growth segments
                var emergingSegments = marketAnalysis.MarketSegments
                    .Where(s => s.GrowthTrend == "High Growth" && s.CompanyCount < 15)
                    .OrderByDescending(s => s.AverageScore)
                    .Take(2)
                    .Select(s => $"{s.SegmentName} (Emerging) - {s.CompanyCount} companies")
                    .ToList();

                opportunities.AddRange(emergingSegments);

                // Add global insights
                opportunities.AddRange(marketAnalysis.GlobalInsights.InvestmentOpportunities.Take(2));

                _logger.LogInformation("✅ Found {Count} investment opportunities", opportunities.Count);
                return opportunities.Distinct().Take(7).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to get investment opportunities");
                return new List<string>();
            }
        }

        public async Task<List<string>> GetEmergingTrendsAsync()
        {
            try
            {
                _logger.LogInformation("📈 Analyzing emerging trends");

                // Get market segments
                var marketAnalysis = await AnalyzeMarketSegmentsAsync(8);
                
                if (!marketAnalysis.Success)
                {
                    return new List<string>();
                }

                var trends = new List<string>();

                // High growth segments with smaller company counts (emerging)
                var emergingTrends = marketAnalysis.MarketSegments
                    .Where(s => s.GrowthTrend == "High Growth" && s.CompanyCount <= 10)
                    .OrderByDescending(s => s.AverageScore)
                    .Select(s => s.SegmentName)
                    .Take(3)
                    .ToList();

                trends.AddRange(emergingTrends);

                // Segments with unique characteristics
                var innovativeTrends = marketAnalysis.MarketSegments
                    .Where(s => s.KeyCharacteristics.Any(k => 
                        k.Contains("AI") || k.Contains("Quantum") || k.Contains("Blockchain") || 
                        k.Contains("Renewable") || k.Contains("Biotechnology")))
                    .OrderByDescending(s => s.AverageScore)
                    .Select(s => s.SegmentName)
                    .Take(3)
                    .ToList();

                trends.AddRange(innovativeTrends);

                // Add global emerging trend
                if (!string.IsNullOrEmpty(marketAnalysis.GlobalInsights.EmergingTrend))
                {
                    trends.Add(marketAnalysis.GlobalInsights.EmergingTrend);
                }

                // Add some predefined emerging trends based on current market
                var additionalTrends = new List<string>
                {
                    "Sustainable Technology",
                    "Digital Health",
                    "Web3 & Metaverse",
                    "Autonomous Systems",
                    "Clean Energy Storage"
                };

                trends.AddRange(additionalTrends.Take(2));

                _logger.LogInformation("✅ Identified {Count} emerging trends", trends.Count);
                return trends.Distinct().Take(8).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to get emerging trends");
                return new List<string>();
            }
        }

        public async Task<Dictionary<string, int>> GetSectorDistributionAsync()
        {
            try
            {
                _logger.LogInformation("📊 Analyzing sector distribution");

                // Get market segments
                var marketAnalysis = await AnalyzeMarketSegmentsAsync(10);
                
                if (!marketAnalysis.Success)
                {
                    return new Dictionary<string, int>();
                }

                var distribution = new Dictionary<string, int>();

                // Get distribution from market segments
                foreach (var segment in marketAnalysis.MarketSegments)
                {
                    distribution[segment.SegmentName] = segment.CompanyCount;
                }

                // Add global insights distribution if available
                if (marketAnalysis.GlobalInsights.SectorDistribution.Any())
                {
                    foreach (var sector in marketAnalysis.GlobalInsights.SectorDistribution)
                    {
                        if (!distribution.ContainsKey(sector.Key))
                        {
                            distribution[sector.Key] = sector.Value;
                        }
                    }
                }

                // Sort by company count descending
                var sortedDistribution = distribution
                    .OrderByDescending(kv => kv.Value)
                    .ToDictionary(kv => kv.Key, kv => kv.Value);

                _logger.LogInformation("✅ Analyzed distribution across {Count} sectors", sortedDistribution.Count);
                return sortedDistribution;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to get sector distribution");
                return new Dictionary<string, int>();
            }
        }
    }

    // Helper classes for ML.NET clustering
    public class ClusteringInputData
    {
        [VectorType]
        public float[] Features { get; set; } = Array.Empty<float>();
    }

    public class ClusteringPrediction
    {
        [ColumnName("PredictedLabel")]
        public uint PredictedClusterId { get; set; }

        [ColumnName("Score")]
        public float[] Distances { get; set; } = Array.Empty<float>();
    }
}
