using ZefsjulaApi.Models.AI;
using ZefsjulaApi.Models.DTO;

namespace ZefsjulaApi.Services.AI_Interface
{
    public interface IEmbeddingService
    {
        Task<float[]> GenerateEmbeddingAsync(string text);
        Task<List<CompanyEmbedding>> GenerateCompanyEmbeddingsAsync(List<CompanyDto> companies);
        Task<double> CalculateSimilarityAsync(float[] embedding1, float[] embedding2);
    }
}
