namespace ZefsjulaApi.Models.AI
{
    public class CompanyEmbedding
    {
        public int CompanyId { get; set; }
        public string CompanyName { get; set; } = "";
        public float[] Embedding { get; set; } = Array.Empty<float>();
        public int ClusterId { get; set; }
        public double SimilarityScore { get; set; }
    }
}
