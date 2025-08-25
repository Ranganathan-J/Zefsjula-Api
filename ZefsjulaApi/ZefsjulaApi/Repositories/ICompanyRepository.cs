using ZefsjulaApi.Models;

namespace ZefsjulaApi.Repositories
{
    public interface ICompanyRepository
    {
        Task<IEnumerable<Company>> GetAllAsync();
        Task<IEnumerable<Company>> GetPagedAsync(int pageNumber, int pageSize);
        Task<int> GetTotalCountAsync();
        Task<Company?> GetByIdAsync(int id);
        Task<IEnumerable<Company>> GetByStatusAsync(string status);
        Task<IEnumerable<Company>> GetByCountryAsync(string countryCode);
        Task<IEnumerable<Company>> GetByFundingRangeAsync(decimal minFunding, decimal maxFunding);


        Task<Company> CreateAsync (Company company);



        Task<Company?> GetByNameAsync(string name);
        Task<bool> UpdateByIdAsync(int id, Company company);
        Task<bool> UpdateByNameAsync(string name, Company company);


        Task<bool> DeleteByIdAsync(int id);
        Task<bool> DeleteByNameAsync(string name);
    }
}

