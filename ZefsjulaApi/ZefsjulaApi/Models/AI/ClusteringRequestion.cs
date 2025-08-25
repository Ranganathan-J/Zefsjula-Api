namespace ZefsjulaApi.Models.AI
{
    public class ClusteringRequestion
    {
        public string Algorithm { get; set; } = "kmeans";
        public int Numberofcluster {  get; set; }
        public bool usePCA { get; set; }

        public List<int>? CompanyId { get; set; }
    }
}
