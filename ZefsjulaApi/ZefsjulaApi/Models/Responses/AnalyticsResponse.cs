namespace ZefsjulaApi.Models.Responses
{
    public class AnalyticsResponse<T> : ApiResponse<T>
    {
        public Dictionary<string, object> Analytics { get; set; } = new();
        public TimeSpan ProcessingTime { get; set; }
        public int RecordCount { get; set; }

        public static AnalyticsResponse<T> CreateAnalyticsResponse(
            T data,
            Dictionary<string, object> analytics,
            TimeSpan processingTime,
            int recordCount,
            string message = "Data retrieved with analytics")
        {
            return new AnalyticsResponse<T>
            {
                Success = true,
                Message = message,
                Data = data,
                Analytics = analytics,
                ProcessingTime = processingTime,
                RecordCount = recordCount
            };
        }
    }
}

