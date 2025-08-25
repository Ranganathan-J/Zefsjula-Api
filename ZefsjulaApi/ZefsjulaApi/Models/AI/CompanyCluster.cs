namespace ZefsjulaApi.Models.AI
{
    public class CompanyCluster
    {
        public int ClusterId { get; set; }
        public string ClusterName { get; set; } = "";
        public List<CompanyEmbedding> Companies { get; set; } = new();
        public Dictionary<string, object> Characteristics { get; set; } = new();
    }
}
