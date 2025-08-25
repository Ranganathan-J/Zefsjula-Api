using ZefsjulaApi.Models;
using ZefsjulaApi.Models.DTO;

namespace ZefsjulaApi.Mappings
{
    public static class Mapper
    {
        public static CompanyDto ToCompanyDto(this Company company, int companyId)
        {
            return new CompanyDto
            {
                CompanyId = companyId,
                Name = company.Name ?? string.Empty,
                CategoryList = company.CategoryList,
                FundingTotalUsd = (decimal?)company.FundingTotalUsd,
                Status = company.Status,
                CountryCode = company.CountryCode,
                City = company.City,
                FundingRounds = (int?)company.FundingRounds,
                FoundedAt = company.FoundedAt,
                FirstFundingAt = company.FirstFundingAt,
                LastFundingAt = company.LastFundingAt,
                DaysBetweenFirstLastFunding = company.FirstFundingAt != null && company.LastFundingAt != null
                    ? company.LastFundingAt.Value.DayNumber - company.FirstFundingAt.Value.DayNumber
                    : null,
                FundingCategory = company.FundingTotalUsd switch
                {
                    null => "No Funding",
                    <= 1000000 => "Early Stage",
                    <= 10000000 => "Growth Stage",
                    _ => "Late Stage"
                }
            };
        }

        public static IEnumerable<CompanyDto> ToCompanyDtoList(this IEnumerable<Company> companies)
        {
            return companies.Select((company, index) => company.ToCompanyDto(index + 1));
        }

        public static IEnumerable<CompanyDto> ToCompanyDtoList(this IEnumerable<Company> companies, int startIndex)
        {
            return companies.Select((company, index) => company.ToCompanyDto(startIndex + index + 1));
        }

        public static Company ToCompany(this CreateCompanyDto createDto)
        {
            return new Company
            {
                Name = createDto.Name,
                HomepageUrl = createDto.HomepageUrl,
                CategoryList = createDto.CategoryList,
                FundingTotalUsd = (double?)createDto.FundingTotalUsd,
                Status = createDto.Status,
                CountryCode = createDto.CountryCode,
                StateCode = createDto.StateCode,
                City = createDto.City,
                FundingRounds = (long?)createDto.FundingRounds,
                FoundedAt = createDto.FoundedAt,
                FirstFundingAt = createDto.FirstFundingAt,
                LastFundingAt = createDto.LastFundingAt
            };
        }

        public static Company ToCompany(this UpdateCompanyDto updateDto, Company existingCompany)
        {
            return new Company
            {
                Name = updateDto.Name ?? existingCompany.Name,
                HomepageUrl = updateDto.HomepageUrl ?? existingCompany.HomepageUrl,
                CategoryList = updateDto.CategoryList ?? existingCompany.CategoryList,
                FundingTotalUsd = updateDto.FundingTotalUsd != null ? (double?)updateDto.FundingTotalUsd : existingCompany.FundingTotalUsd,
                Status = updateDto.Status ?? existingCompany.Status,
                CountryCode = updateDto.CountryCode ?? existingCompany.CountryCode,
                StateCode = updateDto.StateCode ?? existingCompany.StateCode,
                City = updateDto.City ?? existingCompany.City,
                FundingRounds = updateDto.FundingRounds != null ? (long?)updateDto.FundingRounds : existingCompany.FundingRounds,
                FoundedAt = updateDto.FoundedAt ?? existingCompany.FoundedAt,
                FirstFundingAt = updateDto.FirstFundingAt ?? existingCompany.FirstFundingAt,
                LastFundingAt = updateDto.LastFundingAt ?? existingCompany.LastFundingAt
            };
        }
    }
}
