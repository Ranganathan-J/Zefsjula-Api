using System.ComponentModel.DataAnnotations;

namespace ZefsjulaApi.Models.DTO
{
    public class UpdateCompanyDto
    {
        [StringLength(255, ErrorMessage = "Company name cannot exceed 255 characters")]
        public string? Name { get; set; }

        [Url(ErrorMessage = "Please provide a valid URL")]
        [StringLength(500, ErrorMessage = "Homepage URL cannot exceed 500 characters")]
        public string? HomepageUrl { get; set; }

        [StringLength(500, ErrorMessage = "Category list cannot exceed 500 characters")]
        public string? CategoryList { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Funding amount must be positive")]
        public decimal? FundingTotalUsd { get; set; }

        [StringLength(50, ErrorMessage = "Status cannot exceed 50 characters")]
        public string? Status { get; set; }

        [StringLength(10, ErrorMessage = "Country code cannot exceed 10 characters")]
        public string? CountryCode { get; set; }

        [StringLength(100, ErrorMessage = "State code cannot exceed 100 characters")]
        public string? StateCode { get; set; }

        [StringLength(100, ErrorMessage = "City cannot exceed 100 characters")]
        public string? City { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Funding rounds must be positive")]
        public int? FundingRounds { get; set; }

        public DateOnly? FoundedAt { get; set; }
        public DateOnly? FirstFundingAt { get; set; }
        public DateOnly? LastFundingAt { get; set; }
    }
}
