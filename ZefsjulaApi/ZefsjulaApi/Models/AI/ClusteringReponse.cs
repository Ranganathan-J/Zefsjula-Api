namespace ZefsjulaApi.Models.AI
{
    public class ClusteringReponse
    {

        public string Algorithm { get; set; } = "";
        public int NumberOfClusters { get; set; }
        public double SilhouetteScore { get; set; }
        public List<CompanyCluster> Clusters { get; set; } = new();
        public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
    }
}
