# .NET 8 Web API Project Structure for Startup Database

## Recommended Project Structure

```
StartupAPI/
├── StartupAPI.sln
├── src/
│   ├── StartupAPI/
│   │   ├── Controllers/
│   │   │   ├── CompaniesController.cs
│   │   │   ├── AnalyticsController.cs
│   │   │   ├── FundingController.cs
│   │   │   └── GeographicsController.cs
│   │   ├── Models/
│   │   │   ├── Company.cs
│   │   │   ├── DTOs/
│   │   │   │   ├── CompanyDto.cs
│   │   │   │   ├── CompanyCreateDto.cs
│   │   │   │   ├── CompanyUpdateDto.cs
│   │   │   │   └── FundingAnalyticsDto.cs
│   │   │   └── Responses/
│   │   │       ├── ApiResponse.cs
│   │   │       ├── PagedResponse.cs
│   │   │       └── AnalyticsResponse.cs
│   │   ├── Services/
│   │   │   ├── Interfaces/
│   │   │   │   ├── ICompanyService.cs
│   │   │   │   ├── IAnalyticsService.cs
│   │   │   │   └── IFundingService.cs
│   │   │   ├── CompanyService.cs
│   │   │   ├── AnalyticsService.cs
│   │   │   └── FundingService.cs
│   │   ├── Data/
│   │   │   ├── StartupDbContext.cs
│   │   │   ├── Configurations/
│   │   │   │   └── CompanyConfiguration.cs
│   │   │   └── Repositories/
│   │   │       ├── Interfaces/
│   │   │       │   └── ICompanyRepository.cs
│   │   │       └── CompanyRepository.cs
│   │   ├── Middleware/
│   │   │   ├── ErrorHandlingMiddleware.cs
│   │   │   └── RateLimitingMiddleware.cs
│   │   ├── Extensions/
│   │   │   ├── ServiceCollectionExtensions.cs
│   │   │   └── QueryExtensions.cs
│   │   ├── Filters/
│   │   │   └── CompanyQueryFilter.cs
│   │   ├── Mappings/
│   │   │   └── AutoMapperProfile.cs
│   │   ├── Program.cs
│   │   ├── appsettings.json
│   │   ├── appsettings.Development.json
│   │   └── StartupAPI.csproj
│   └── StartupAPI.Tests/
│       ├── Controllers/
│       ├── Services/
│       └── StartupAPI.Tests.csproj
└── README.md
```

## Key Components

### 1. Company Model (Models/Company.cs)
```csharp
public class Company
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
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

### 2. Main Endpoints Overview

#### CompaniesController
- Basic CRUD operations
- Search and filtering
- Pagination support

#### AnalyticsController
- Dashboard data
- Statistical analysis
- Trend analysis
- Market insights

#### FundingController
- Funding analysis
- Investment trends
- Valuation insights
- Round analysis

#### GeographicsController
- Geographic distribution
- Regional analysis
- Country/city statistics
- Heatmap data

### 3. Key Features to Implement

#### Pagination & Filtering
```csharp
GET /api/companies?page=1&pageSize=20&country=USA&minFunding=1000000
```

#### Advanced Search
```csharp
GET /api/companies/search?q=fintech&category=Financial Services&status=operating
```

#### Analytics Endpoints
```csharp
GET /api/analytics/funding-by-year
GET /api/analytics/top-categories
GET /api/analytics/geographic-distribution
GET /api/analytics/success-rates
```

#### Export Capabilities
```csharp
GET /api/companies/export?format=csv
GET /api/companies/export?format=excel
GET /api/analytics/report/pdf
```

## Technology Stack Recommendations

### Core Technologies
- **.NET 8** - Latest framework
- **Entity Framework Core** - ORM for database operations
- **SQL Server** - Database (already set up)
- **AutoMapper** - Object mapping
- **Swagger/OpenAPI** - API documentation

### Additional Packages
- **Microsoft.EntityFrameworkCore.SqlServer** - SQL Server provider
- **Microsoft.EntityFrameworkCore.Tools** - EF migrations
- **AutoMapper.Extensions.Microsoft.DependencyInjection** - DI integration
- **FluentValidation** - Input validation
- **Serilog** - Structured logging
- **Microsoft.AspNetCore.RateLimiting** - Rate limiting
- **Microsoft.AspNetCore.Authentication.JwtBearer** - JWT authentication (if needed)

### Performance & Caching
- **Microsoft.Extensions.Caching.Memory** - In-memory caching
- **Microsoft.Extensions.Caching.StackExchangeRedis** - Redis caching
- **ResponseCaching** - HTTP response caching

## Sample API Responses

### Company List Response
```json
{
  "data": [
    {
      "companyId": 1,
      "name": "Airtable",
      "homepageUrl": "https://airtable.com",
      "categoryList": "SaaS|Productivity",
      "fundingTotalUsd": 735000000,
      "status": "operating",
      "countryCode": "USA",
      "city": "San Francisco",
      "fundingRounds": 5,
      "foundedAt": "2012-01-01",
      "firstFundingAt": "2013-05-15",
      "lastFundingAt": "2021-12-15"
    }
  ],
  "pagination": {
    "currentPage": 1,
    "pageSize": 20,
    "totalPages": 3172,
    "totalRecords": 63434,
    "hasNext": true,
    "hasPrevious": false
  },
  "success": true,
  "message": "Companies retrieved successfully"
}
```

### Analytics Response
```json
{
  "data": {
    "totalCompanies": 63434,
    "totalFunding": 2847392847392,
    "averageFunding": 44891234,
    "topCategories": [
      {"category": "Software", "count": 3989},
      {"category": "Biotechnology", "count": 3612},
      {"category": "E-Commerce", "count": 1329}
    ],
    "fundingByYear": {
      "2020": 123456789,
      "2021": 234567890,
      "2022": 345678901
    },
    "geographicDistribution": {
      "USA": 35953,
      "GBR": 3484,
      "CAN": 1846
    }
  },
  "success": true,
  "timestamp": "2024-01-15T10:30:00Z"
}
```

## Getting Started Commands

### 1. Create the Project
```bash
dotnet new webapi -n StartupAPI
cd StartupAPI
```

### 2. Add Required Packages
```bash
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
dotnet add package Microsoft.EntityFrameworkCore.Tools
dotnet add package AutoMapper.Extensions.Microsoft.DependencyInjection
dotnet add package FluentValidation.AspNetCore
dotnet add package Serilog.AspNetCore
```

### 3. Connection String (appsettings.json)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=StartupDB;Trusted_Connection=true;TrustServerCertificate=true;"
  }
}
```

This structure provides a solid foundation for building a comprehensive startup data API with .NET 8!