namespace ZefsjulaApi.Models.DTO
{
    public class CompanyDto
    {
        public int CompanyId { get; set; }

        public string Name { get; set; } = null!;

        public string? CategoryList { get; set; }

        public decimal? FundingTotalUsd { get; set; }

        public string? Status { get; set; }

        public string? CountryCode { get; set; }

        public string? City { get; set; }

        public int? FundingRounds { get; set; }

        public DateOnly? FoundedAt { get; set; }

        public DateOnly? FirstFundingAt { get; set; }

        public DateOnly? LastFundingAt { get; set; }

        public int? DaysBetweenFirstLastFunding { get; set; }

        public string FundingCategory { get; set; } = null!;
    }
}
