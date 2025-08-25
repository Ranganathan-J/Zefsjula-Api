using System.Diagnostics;
using ZefsjulaApi.Exceptions;
using ZefsjulaApi.Mappings;
using ZefsjulaApi.Models.DTO;
using ZefsjulaApi.Models.Responses;
using ZefsjulaApi.Repositories;

namespace ZefsjulaApi.Services
{
    public class CompanyService : ICompanyService
    {
        private readonly ICompanyRepository _companyRepository;
        private readonly ILogger<CompanyService> _logger;

        public CompanyService(ICompanyRepository companyRepository, ILogger<CompanyService> logger)
        {
            _companyRepository = companyRepository;
            _logger = logger;
        }

        public async Task<ApiResponse<IEnumerable<CompanyDto>>> GetAllCompaniesAsync()
        {
            var stopwatch = Stopwatch.StartNew();

            var companies = await _companyRepository.GetAllAsync();
            var companyDtos = companies.ToCompanyDtoList();

            stopwatch.Stop();

            return ApiResponse<IEnumerable<CompanyDto>>.SuccessResult(
                companyDtos,
                $"Successfully retrieved {companyDtos.Count()} companies in {stopwatch.ElapsedMilliseconds}ms"
            );
        }

        public async Task<PagedResponse<CompanyDto>> GetPagedCompaniesAsync(int pageNumber, int pageSize)
        {

            var totalRecords = await _companyRepository.GetTotalCountAsync();
            var companies = await _companyRepository.GetPagedAsync(pageNumber, pageSize);

            var startIndex = (pageNumber - 1) * pageSize;
            var companyDtos = companies.ToCompanyDtoList(startIndex);

            return PagedResponse<CompanyDto>.CreatePagedResponse(
                companyDtos,
                pageNumber,
                pageSize,
                totalRecords,
                $"Retrieved page {pageNumber} of {Math.Ceiling((double)totalRecords / pageSize)} pages"
            );

        }

        public async Task<AnalyticsResponse<IEnumerable<CompanyDto>>> GetCompaniesWithAnalyticsAsync()
        {
            var stopwatch = Stopwatch.StartNew();

            var companies = await _companyRepository.GetAllAsync();
            var companyDtos = companies.ToCompanyDtoList().ToList();

            stopwatch.Stop();

            var analytics = CalculateAnalytics(companyDtos);

            return AnalyticsResponse<IEnumerable<CompanyDto>>.CreateAnalyticsResponse(
                companyDtos,
                analytics,
                stopwatch.Elapsed,
                companyDtos.Count,
                "Companies retrieved with detailed analytics"
            );
        }

        public async Task<ApiResponse<CompanyDto>> GetCompanyByIdAsync(int id)
        {
            _logger.LogInformation("Retrieving company with ID: {CompanyId}", id);
            var company = await _companyRepository.GetByIdAsync(id);

            if (company == null)
            {
                // return ApiResponse<CompanyDto>.ErrorResult(
                //     $"Company with ID {id} not found",
                //     "COMPANY_NOT_FOUND"
                // );
                // using middleware global exception handler
                throw new NotFoundException($"Company with ID {id} not found");
            }

            var companyDto = company.ToCompanyDto(id);

            _logger.LogInformation("Successfully retrieved company: {CompanyName} (ID: {CompanyId})",
            companyDto.Name, companyDto.CompanyId);

            return ApiResponse<CompanyDto>.SuccessResult(
                companyDto,
                $"Company with ID {id} retrieved successfully"
            );
            
        }

        public async Task<ApiResponse<IEnumerable<CompanyDto>>> GetCompaniesByStatusAsync(string status)
        {

            var companies = await _companyRepository.GetByStatusAsync(status);
            var companyDtos = companies.ToCompanyDtoList();

            return ApiResponse<IEnumerable<CompanyDto>>.SuccessResult(
                companyDtos,
                $"Successfully retrieved {companyDtos.Count()} companies with status '{status}'"
            );
            
        }

        public async Task<ApiResponse<IEnumerable<CompanyDto>>> GetCompaniesByCountryAsync(string countryCode)
        {

            var companies = await _companyRepository.GetByCountryAsync(countryCode);
            var companyDtos = companies.ToCompanyDtoList();

            return ApiResponse<IEnumerable<CompanyDto>>.SuccessResult(
                companyDtos,
                $"Successfully retrieved {companyDtos.Count()} companies from '{countryCode}'"
            );
        }

        public async Task<ApiResponse<IEnumerable<CompanyDto>>> GetCompaniesByFundingRangeAsync(decimal minFunding, decimal maxFunding)
        {
            var companies = await _companyRepository.GetByFundingRangeAsync(minFunding, maxFunding);
            var companyDtos = companies.ToCompanyDtoList();

            return ApiResponse<IEnumerable<CompanyDto>>.SuccessResult(
                companyDtos,
                $"Successfully retrieved {companyDtos.Count()} companies with funding between ${minFunding:N0} and ${maxFunding:N0}"
            );
        }

        private Dictionary<string, object> CalculateAnalytics(IList<CompanyDto> companies)
        {
            return new Dictionary<string, object>
            {
                ["totalCompanies"] = companies.Count,
                ["fundingCategories"] = companies.GroupBy(c => c.FundingCategory)
                    .ToDictionary(g => g.Key, g => g.Count()),
                ["averageFunding"] = companies.Where(c => c.FundingTotalUsd.HasValue)
                    .DefaultIfEmpty()
                    .Average(c => c?.FundingTotalUsd ?? 0),
                ["totalFunding"] = companies.Where(c => c.FundingTotalUsd.HasValue)
                    .Sum(c => c.FundingTotalUsd.Value),
                ["topCountries"] = companies.Where(c => !string.IsNullOrEmpty(c.CountryCode))
                    .GroupBy(c => c.CountryCode)
                    .OrderByDescending(g => g.Count())
                    .Take(5)
                    .ToDictionary(g => g.Key!, g => g.Count()),
                ["statusDistribution"] = companies.Where(c => !string.IsNullOrEmpty(c.Status))
                    .GroupBy(c => c.Status)
                    .ToDictionary(g => g.Key!, g => g.Count()),
                ["averageFundingRounds"] = companies.Where(c => c.FundingRounds.HasValue)
                    .DefaultIfEmpty()
                    .Average(c => c?.FundingRounds ?? 0)
            };
        }


        public async Task<ApiResponse<CompanyDto>> CreateCompanyAsync(CreateCompanyDto createCompanyDto)
        {
            _logger.LogInformation("Creating new company with name: {CompanyName}", createCompanyDto.Name);
            var entity = await _companyRepository.CreateAsync(createCompanyDto.ToCompany());

            var totalCount = await _companyRepository.GetTotalCountAsync();

            var entityDto = entity.ToCompanyDto(totalCount);
            _logger.LogInformation("Successfully created company with ID: {CompanyId}", entityDto.CompanyId);
            return ApiResponse<CompanyDto>.SuccessResult(
                entityDto, "company data created"
                );
        }

        public async Task<ApiResponse<CompanyDto>> UpdateCompanyByIdAsync(int id, UpdateCompanyDto updateCompanyDto)
        {

            _logger.LogInformation("Updating company with ID: {CompanyId}", id);
            // Get existing company
            var existingCompany = await _companyRepository.GetByIdAsync(id);
            if (existingCompany == null)
            {
                //return ApiResponse<CompanyDto>.ErrorResult(
                //    $"Company with ID {id} not found",
                //    "COMPANY_NOT_FOUND"
                //);
                //using global exception handler
                throw new NotFoundException($"Company with ID {id} not found");
            }

            // Map update DTO to company entity using existing data as fallback
            var updatedCompany = updateCompanyDto.ToCompany(existingCompany);

            // Update the company
            var success = await _companyRepository.UpdateByIdAsync(id, updatedCompany);

            if (!success)
            {
                throw new NotFoundException($"Company with ID {id} not found");
            }

            // Return the updated company data
            var companyDto = updatedCompany.ToCompanyDto(id);

            _logger.LogInformation("Successfully retrieved company: {CompanyName} (ID: {CompanyId})",
            companyDto.Name, companyDto.CompanyId);

            return ApiResponse<CompanyDto>.SuccessResult(
                companyDto,
                "Company updated successfully"
            );
        }

        public async Task<ApiResponse<CompanyDto>> UpdateCompanyByNameAsync(string name, UpdateCompanyDto updateCompanyDto)
        {
            // Get existing company by name
            var existingCompany = await _companyRepository.GetByNameAsync(name);
            if (existingCompany == null)
            {
                //return ApiResponse<CompanyDto>.ErrorResult(
                //    $"Company with name '{name}' not found",
                //    "COMPANY_NOT_FOUND"
                //);
                throw new NotFoundException($"Company with name '{name}' not found");
            }

            // Map update DTO to company entity using existing data as fallback
            var updatedCompany = updateCompanyDto.ToCompany(existingCompany);

            // Update the company
            var success = await _companyRepository.UpdateByNameAsync(name, updatedCompany);

            if (!success)
            {
                //return ApiResponse<CompanyDto>.ErrorResult(
                //    $"Failed to update company with name '{name}'",
                //    "COMPANY_UPDATE_FAILED"
                //);
                throw new BadRequestException($"Failed to update company with name '{name}'");
            }

            // Generate a CompanyId for response (you might want to improve this)
            var totalCount = await _companyRepository.GetTotalCountAsync();
            var companyDto = updatedCompany.ToCompanyDto(totalCount);

            return ApiResponse<CompanyDto>.SuccessResult(
                companyDto,
                "Company updated successfully"
            );
        }

        public async Task<ApiResponse<string>> DeleteCompanyByIdAsync(int id)
        {
            // Check if company exists first
            var existingCompany = await _companyRepository.GetByIdAsync(id);
            if (existingCompany == null)
            {
                //return ApiResponse<string>.ErrorResult(
                //    $"Company with ID {id} not found",
                //    "COMPANY_NOT_FOUND"
                //);
                throw new NotFoundException($"Company with ID {id} not found");
            }

            // Delete the company
            var success = await _companyRepository.DeleteByIdAsync(id);

            if (!success)
            {
                throw new NotFoundException($"Company with ID {id} not found");
            }

            return ApiResponse<string>.SuccessResult(
                $"Company with ID {id} deleted successfully",
                "Company deleted successfully"
            );
        }

        public async Task<ApiResponse<string>> DeleteCompanyByNameAsync(string name)
        {
            
            // Check if company exists first
            var existingCompany = await _companyRepository.GetByNameAsync(name);
            if (existingCompany == null)
            {
                //return ApiResponse<string>.ErrorResult(
                //    $"Company with name '{name}' not found",
                //    "COMPANY_NOT_FOUND"
                //);
                throw new NotFoundException($"Company with name '{name}' not found");
            }

            // Delete the company
            var success = await _companyRepository.DeleteByNameAsync(name);

            if (!success)
            {
                //return ApiResponse<string>.ErrorResult(
                //    $"Failed to delete company with name '{name}'",
                //    "COMPANY_DELETE_FAILED"
                //);
                throw new BadRequestException($"Failed to delete company with name '{name}'");
            }
            _logger.LogInformation("Successfully deleted company with name: {CompanyName}", name);

            return ApiResponse<string>.SuccessResult(
                $"Company '{name}' deleted successfully",
                "Company deleted successfully"
            );
        }
    }
}

