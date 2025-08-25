using System;
using System.Collections.Generic;

namespace ZefsjulaApi.Models;

public partial class Company
{
    public string? Name { get; set; }

    public string? HomepageUrl { get; set; }

    public string? CategoryList { get; set; }

    public double? FundingTotalUsd { get; set; }

    public string? Status { get; set; }

    public string? CountryCode { get; set; }

    public string? StateCode { get; set; }

    public string? City { get; set; }

    public long? FundingRounds { get; set; }

    public DateOnly? FoundedAt { get; set; }

    public DateOnly? FirstFundingAt { get; set; }

    public DateOnly? LastFundingAt { get; set; }
}
