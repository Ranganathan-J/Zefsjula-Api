using ZefsjulaApi.Models.AI;

namespace ZefsjulaApi.Services.AI_Interface
{
    public interface IMarketIntelligenceService
    {
        Task<MarketIntelligenceResponse> AnalyzeMarketSegmentsAsync(int numberOfSegments = 8);
        Task<MarketSegment> GetSegmentDetailsAsync(int segmentId);
        Task<List<string>> GetInvestmentOpportunitiesAsync();
        Task<List<string>> GetEmergingTrendsAsync();
        Task<Dictionary<string, int>> GetSectorDistributionAsync();
    }
}
