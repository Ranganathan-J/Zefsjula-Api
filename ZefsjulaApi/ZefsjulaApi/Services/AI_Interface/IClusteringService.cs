using ZefsjulaApi.Models.AI;
using ZefsjulaApi.Models.DTO;

namespace ZefsjulaApi.Services.AI_Interface
{
    public interface IClusteringService
    {
        Task<ClusteringReponse> PerformClusteringAsync(ClusteringReponse request);
        Task<List<CompanyDto>> FindSimilarCompaniesAsync(int companyId, int maxResults = 10);
        Task<ClusteringReponse> GetCachedClusteringResultAsync(string cacheKey);
    }
}
