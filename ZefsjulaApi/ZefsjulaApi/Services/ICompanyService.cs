using ZefsjulaApi.Models.DTO;
using ZefsjulaApi.Models.Responses;

namespace ZefsjulaApi.Services
{
    public interface ICompanyService
    {
        Task<ApiResponse<IEnumerable<CompanyDto>>> GetAllCompaniesAsync();
        Task<PagedResponse<CompanyDto>> GetPagedCompaniesAsync(int pageNumber, int pageSize);
        Task<AnalyticsResponse<IEnumerable<CompanyDto>>> GetCompaniesWithAnalyticsAsync();
        Task<ApiResponse<CompanyDto>> GetCompanyByIdAsync(int id);
        Task<ApiResponse<IEnumerable<CompanyDto>>> GetCompaniesByStatusAsync(string status);
        Task<ApiResponse<IEnumerable<CompanyDto>>> GetCompaniesByCountryAsync(string countryCode);
        Task<ApiResponse<IEnumerable<CompanyDto>>> GetCompaniesByFundingRangeAsync(decimal minFunding, decimal maxFunding);

        Task <ApiResponse<CompanyDto>> CreateCompanyAsync(CreateCompanyDto createCompanyDto);


        Task<ApiResponse<CompanyDto>> UpdateCompanyByIdAsync(int id, UpdateCompanyDto updateCompanyDto);
        Task<ApiResponse<CompanyDto>> UpdateCompanyByNameAsync(string name, UpdateCompanyDto updateCompanyDto);

        Task<ApiResponse<string>> DeleteCompanyByIdAsync(int id);
        Task<ApiResponse<string>> DeleteCompanyByNameAsync(string name);
    }
}
