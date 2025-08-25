# Database First Approach - Entity and DTO Creation Guide

## 🗄️ Database First Approach Setup

Since you already have the `StartupDB` database with the `Companies` table, here's how to scaffold entities and create DTOs.

## Step 1: Scaffold Entity from Database

### Install EF Core Tools (if not already installed)
```bash
dotnet tool install --global dotnet-ef
# or update if already installed
dotnet tool update --global dotnet-ef
```

### Scaffold Entity from Existing Database
```bash
# Navigate to your API project directory
cd StartupAPI

# Scaffold the DbContext and Entity from existing database
dotnet ef dbcontext scaffold "Server=localhost;Database=StartupDB;Trusted_Connection=true;TrustServerCertificate=true;" Microsoft.EntityFrameworkCore.SqlServer -o Models -c StartupDbContext --context-dir Data --force
```

### Alternative Scaffold Command (More Specific)
```bash
# Scaffold only specific tables
dotnet ef dbcontext scaffold "Server=localhost;Database=StartupDB;Trusted_Connection=true;TrustServerCertificate=true;" Microsoft.EntityFrameworkCore.SqlServer -o Models -c StartupDbContext --context-dir Data --table Companies --force
```

## Step 2: Generated Entity Structure

After scaffolding, you'll get something like this:

### Generated Company Entity (Models/Company.cs)
```csharp
public partial class Company
{
    public int CompanyId { get; set; }
    public string Name { get; set; } = null!;
    public string? HomepageUrl { get; set; }
    public string? CategoryList { get; set; }
    public decimal? FundingTotalUsd { get; set; }
    public string? Status { get; set; }
    public string? CountryCode { get; set; }
    public string? StateCode { get; set; }
    public string? City { get; set; }
    public int? FundingRounds { get; set; }
    public DateTime? FoundedAt { get; set; }
    public DateTime? FirstFundingAt { get; set; }
    public DateTime? LastFundingAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

### Generated DbContext (Data/StartupDbContext.cs)
```csharp
public partial class StartupDbContext : DbContext
{
    public StartupDbContext(DbContextOptions<StartupDbContext> options) : base(options) { }

    public virtual DbSet<Company> Companies { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Company>(entity =>
        {
            entity.HasKey(e => e.CompanyId);
            entity.Property(e => e.Name).HasMaxLength(255);
            entity.Property(e => e.HomepageUrl).HasMaxLength(500);
            // ... other configurations
        });
    }
}
```

## Step 3: Create DTOs

Now create DTOs for different use cases:

### 1. Company Response DTO (DTOs/CompanyDto.cs)
```csharp
namespace StartupAPI.DTOs
{
    public class CompanyDto
    {
        public int CompanyId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? HomepageUrl { get; set; }
        public string? CategoryList { get; set; }
        public decimal? FundingTotalUsd { get; set; }
        public string? Status { get; set; }
        public string? CountryCode { get; set; }
        public string? StateCode { get; set; }
        public string? City { get; set; }
        public int? FundingRounds { get; set; }
        public DateTime? FoundedAt { get; set; }
        public DateTime? FirstFundingAt { get; set; }
        public DateTime? LastFundingAt { get; set; }
        
        // Computed properties
        public string FundingTotalFormatted => FundingTotalUsd?.ToString("C0") ?? "N/A";
        public int? YearsSinceFoundation => FoundedAt.HasValue ? DateTime.Now.Year - FoundedAt.Value.Year : null;
        public string[] Categories => CategoryList?.Split('|', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
    }
}
```

### 2. Company Summary DTO (DTOs/CompanySummaryDto.cs)
```csharp
namespace StartupAPI.DTOs
{
    public class CompanySummaryDto
    {
        public int CompanyId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? HomepageUrl { get; set; }
        public decimal? FundingTotalUsd { get; set; }
        public string? Status { get; set; }
        public string? CountryCode { get; set; }
        public string? City { get; set; }
        public string? PrimaryCategory { get; set; }
        public string FundingTotalFormatted => FundingTotalUsd?.ToString("C0") ?? "N/A";
    }
}
```

### 3. Company Create DTO (DTOs/CompanyCreateDto.cs)
```csharp
using System.ComponentModel.DataAnnotations;

namespace StartupAPI.DTOs
{
    public class CompanyCreateDto
    {
        [Required]
        [StringLength(255)]
        public string Name { get; set; } = string.Empty;
        
        [Url]
        [StringLength(500)]
        public string? HomepageUrl { get; set; }
        
        [StringLength(500)]
        public string? CategoryList { get; set; }
        
        [Range(0, double.MaxValue)]
        public decimal? FundingTotalUsd { get; set; }
        
        [StringLength(50)]
        public string? Status { get; set; }
        
        [StringLength(10)]
        public string? CountryCode { get; set; }
        
        [StringLength(50)]
        public string? StateCode { get; set; }
        
        [StringLength(100)]
        public string? City { get; set; }
        
        [Range(0, int.MaxValue)]
        public int? FundingRounds { get; set; }
        
        public DateTime? FoundedAt { get; set; }
        public DateTime? FirstFundingAt { get; set; }
        public DateTime? LastFundingAt { get; set; }
    }
}
```

### 4. Company Update DTO (DTOs/CompanyUpdateDto.cs)
```csharp
using System.ComponentModel.DataAnnotations;

namespace StartupAPI.DTOs
{
    public class CompanyUpdateDto
    {
        [Required]
        [StringLength(255)]
        public string Name { get; set; } = string.Empty;
        
        [Url]
        [StringLength(500)]
        public string? HomepageUrl { get; set; }
        
        [StringLength(500)]
        public string? CategoryList { get; set; }
        
        [Range(0, double.MaxValue)]
        public decimal? FundingTotalUsd { get; set; }
        
        [StringLength(50)]
        public string? Status { get; set; }
        
        [StringLength(10)]
        public string? CountryCode { get; set; }
        
        [StringLength(50)]
        public string? StateCode { get; set; }
        
        [StringLength(100)]
        public string? City { get; set; }
        
        [Range(0, int.MaxValue)]
        public int? FundingRounds { get; set; }
        
        public DateTime? FoundedAt { get; set; }
        public DateTime? FirstFundingAt { get; set; }
        public DateTime? LastFundingAt { get; set; }
    }
}
```

### 5. Analytics DTOs (DTOs/AnalyticsDto.cs)
```csharp
namespace StartupAPI.DTOs
{
    public class AnalyticsDashboardDto
    {
        public int TotalCompanies { get; set; }
        public decimal TotalFunding { get; set; }
        public decimal AverageFunding { get; set; }
        public decimal MedianFunding { get; set; }
        public List<CategoryStatsDto> TopCategories { get; set; } = new();
        public List<CountryStatsDto> TopCountries { get; set; } = new();
        public List<FundingTrendDto> FundingTrends { get; set; } = new();
    }

    public class CategoryStatsDto
    {
        public string Category { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal TotalFunding { get; set; }
        public decimal AverageFunding { get; set; }
    }

    public class CountryStatsDto
    {
        public string CountryCode { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal TotalFunding { get; set; }
        public decimal AverageFunding { get; set; }
    }

    public class FundingTrendDto
    {
        public int Year { get; set; }
        public int CompanyCount { get; set; }
        public decimal TotalFunding { get; set; }
        public decimal AverageFunding { get; set; }
    }
}
```

### 6. Query Filter DTOs (DTOs/QueryFilterDto.cs)
```csharp
namespace StartupAPI.DTOs
{
    public class CompanyQueryFilterDto
    {
        public string? Name { get; set; }
        public string? Category { get; set; }
        public string? Status { get; set; }
        public string? CountryCode { get; set; }
        public string? City { get; set; }
        public decimal? MinFunding { get; set; }
        public decimal? MaxFunding { get; set; }
        public int? MinFundingRounds { get; set; }
        public int? MaxFundingRounds { get; set; }
        public DateTime? FoundedAfter { get; set; }
        public DateTime? FoundedBefore { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? SortBy { get; set; } = "Name";
        public string? SortDirection { get; set; } = "asc";
    }
}
```

### 7. Paged Response DTO (DTOs/PagedResponseDto.cs)
```csharp
namespace StartupAPI.DTOs
{
    public class PagedResponseDto<T>
    {
        public List<T> Data { get; set; } = new();
        public PaginationDto Pagination { get; set; } = new();
        public bool Success { get; set; } = true;
        public string? Message { get; set; }
    }

    public class PaginationDto
    {
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public int TotalRecords { get; set; }
        public bool HasNext { get; set; }
        public bool HasPrevious { get; set; }
    }
}
```

## Step 4: AutoMapper Profile

Create mapping between entities and DTOs:

### Mappings/AutoMapperProfile.cs
```csharp
using AutoMapper;
using StartupAPI.Models;
using StartupAPI.DTOs;

namespace StartupAPI.Mappings
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            // Entity to DTO mappings
            CreateMap<Company, CompanyDto>()
                .ForMember(dest => dest.PrimaryCategory, 
                          opt => opt.MapFrom(src => src.CategoryList != null ? 
                                           src.CategoryList.Split('|')[0] : null));

            CreateMap<Company, CompanySummaryDto>()
                .ForMember(dest => dest.PrimaryCategory, 
                          opt => opt.MapFrom(src => src.CategoryList != null ? 
                                           src.CategoryList.Split('|')[0] : null));

            // DTO to Entity mappings
            CreateMap<CompanyCreateDto, Company>()
                .ForMember(dest => dest.CompanyId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow));

            CreateMap<CompanyUpdateDto, Company>()
                .ForMember(dest => dest.CompanyId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow));
        }
    }
}
```

## Step 5: Usage in Controllers

### Example Controller Usage
```csharp
[ApiController]
[Route("api/[controller]")]
public class CompaniesController : ControllerBase
{
    private readonly ICompanyService _companyService;
    private readonly IMapper _mapper;

    public CompaniesController(ICompanyService companyService, IMapper mapper)
    {
        _companyService = companyService;
        _mapper = mapper;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResponseDto<CompanySummaryDto>>> GetCompanies(
        [FromQuery] CompanyQueryFilterDto filter)
    {
        var companies = await _companyService.GetCompaniesAsync(filter);
        var mappedCompanies = _mapper.Map<List<CompanySummaryDto>>(companies.Data);
        
        return Ok(new PagedResponseDto<CompanySummaryDto>
        {
            Data = mappedCompanies,
            Pagination = companies.Pagination,
            Success = true,
            Message = "Companies retrieved successfully"
        });
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CompanyDto>> GetCompany(int id)
    {
        var company = await _companyService.GetCompanyByIdAsync(id);
        if (company == null)
            return NotFound();

        var mappedCompany = _mapper.Map<CompanyDto>(company);
        return Ok(mappedCompany);
    }
}
```

This approach gives you a clean separation between your database entities and API DTOs while leveraging the existing database structure!