namespace ZefsjulaApi.Models.AI
{
    // Enhanced Market Intelligence Models
    public class MarketSegment
    {
        public int SegmentId { get; set; }
        public string SegmentName { get; set; } = "";
        public string Description { get; set; } = "";
        public int CompanyCount { get; set; }
        public List<CompanyEmbedding> Companies { get; set; } = new();
        public List<string> KeyCharacteristics { get; set; } = new();
        public string InvestmentOpportunity { get; set; } = "";
        public string GrowthTrend { get; set; } = "";
        public double AverageScore { get; set; }
        public Dictionary<string, object> Analytics { get; set; } = new();
    }

    public class MarketIntelligenceResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public List<MarketSegment> MarketSegments { get; set; } = new();
        public MarketInsights GlobalInsights { get; set; } = new();
        public DateTime AnalysisDate { get; set; } = DateTime.UtcNow;
        public int TotalCompaniesAnalyzed { get; set; }
    }

    public class MarketInsights
    {
        public string HottestSector { get; set; } = "";
        public string EmergingTrend { get; set; } = "";
        public List<string> InvestmentOpportunities { get; set; } = new();
        public List<string> MarketGaps { get; set; } = new();
        public Dictionary<string, int> SectorDistribution { get; set; } = new();
    }
}
