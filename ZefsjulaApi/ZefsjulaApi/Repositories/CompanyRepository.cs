using Microsoft.EntityFrameworkCore;
using ZefsjulaApi.Data;
using ZefsjulaApi.Models;

namespace ZefsjulaApi.Repositories
{
    public class CompanyRepository : ICompanyRepository
    {
        private readonly StartupDbContext _context;

        public CompanyRepository(StartupDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Company>> GetAllAsync()
        {
            return await _context.Companies.ToListAsync();
        }

        public async Task<IEnumerable<Company>> GetPagedAsync(int pageNumber, int pageSize)
        {
            return await _context.Companies
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> GetTotalCountAsync()
        {
            return await _context.Companies.CountAsync();
        }

        public async Task<Company?> GetByIdAsync(int id)
        {
            // Since Company table has no primary key, we'll use Skip/Take to simulate getting by index
            return await _context.Companies
                .Skip(id - 1)
                .Take(1)
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<Company>> GetByStatusAsync(string status)
        {
            return await _context.Companies
                .Where(c => c.Status == status)
                .ToListAsync();
        }

        public async Task<IEnumerable<Company>> GetByCountryAsync(string countryCode)
        {
            return await _context.Companies
                .Where(c => c.CountryCode == countryCode)
                .ToListAsync();
        }

        public async Task<IEnumerable<Company>> GetByFundingRangeAsync(decimal minFunding, decimal maxFunding)
        {
            return await _context.Companies
                .Where(c => c.FundingTotalUsd >= (double)minFunding && c.FundingTotalUsd <= (double)maxFunding)
                .ToListAsync();
        }

        public async Task<Company> CreateAsync(Company company)
        {
            //_context.Companies.Add(company);
            //await _context.SaveChangesAsync();
            //return company;

            // adding this line to satisfy the EF core condtion in which db doesn't have primary key

            var sql = @"
    IF NOT EXISTS (
        SELECT 1
          FROM Companies
         WHERE Name           = {0}
           AND FoundedAt      = {9}
           AND HomepageURL    = {1}
           -- Add other unique columns as needed
    )
    BEGIN
        INSERT INTO Companies
          (Name, HomepageURL, CategoryList, FundingTotalUSD, Status,
           CountryCode, StateCode, City, FundingRounds,
           FoundedAt, FirstFundingAt, LastFundingAt)
        VALUES
          ({0}, {1}, {2}, {3}, {4},
           {5}, {6}, {7}, {8},
           {9}, {10}, {11})
    END";

            var rowsAffected = await _context.Database
                .ExecuteSqlRawAsync(sql,
                    company.Name,
                    company.HomepageUrl,
                    company.CategoryList,
                    company.FundingTotalUsd,
                    company.Status,
                    company.CountryCode,
                    company.StateCode,
                    company.City,
                    company.FundingRounds,
                    company.FoundedAt,
                    company.FirstFundingAt,
                    company.LastFundingAt);

            // Return the original company object (service layer needs it for mapping)
            return company;


        }

        public async Task<Company?> GetByNameAsync(string name)
        {
            return await _context.Companies.Where(c => c.Name == name).FirstOrDefaultAsync();
        }

        public async Task<bool> UpdateByIdAsync(int id, Company company)
        {
                        var sql = @"
                UPDATE Companies 
                SET Name = {1}, HomepageURL = {2}, CategoryList = {3}, FundingTotalUSD = {4}, 
                    Status = {5}, CountryCode = {6}, StateCode = {7}, City = {8}, 
                    FundingRounds = {9}, FoundedAt = {10}, FirstFundingAt = {11}, LastFundingAt = {12}
                WHERE Id = (
                    SELECT Id FROM (
                        SELECT ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) as Id, Name
                        FROM Companies
                    ) ranked
                    WHERE ranked.Id = {0}
                )";

            var rowsAffected = await _context.Database.ExecuteSqlRawAsync(sql,
                id, company.Name, company.HomepageUrl, company.CategoryList, company.FundingTotalUsd,
                company.Status, company.CountryCode, company.StateCode, company.City,
                company.FundingRounds, company.FoundedAt, company.FirstFundingAt, company.LastFundingAt);

            return rowsAffected > 0;
        }

        public async Task<bool> UpdateByNameAsync(string name, Company company)
        {
                    var sql = @"
            UPDATE Companies 
            SET Name = {1}, HomepageURL = {2}, CategoryList = {3}, FundingTotalUSD = {4}, 
                Status = {5}, CountryCode = {6}, StateCode = {7}, City = {8}, 
                FundingRounds = {9}, FoundedAt = {10}, FirstFundingAt = {11}, LastFundingAt = {12}
            WHERE Name = {0}";

            var rowsAffected = await _context.Database.ExecuteSqlRawAsync(sql,
                name, company.Name, company.HomepageUrl, company.CategoryList, company.FundingTotalUsd,
                company.Status, company.CountryCode, company.StateCode, company.City,
                company.FundingRounds, company.FoundedAt, company.FirstFundingAt, company.LastFundingAt);

            return rowsAffected > 0;
        }

        public async Task<bool> DeleteByIdAsync(int id)
        {
                        var sql = @"
                WITH NumberedCompanies AS (
                    SELECT *, ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) as RowNum
                    FROM Companies
                )
                DELETE FROM Companies 
                WHERE Name IN (
                    SELECT Name FROM NumberedCompanies WHERE RowNum = {0}
                )
                AND FoundedAt IN (
                    SELECT FoundedAt FROM NumberedCompanies WHERE RowNum = {0}
                )";

            var rowsAffected = await _context.Database.ExecuteSqlRawAsync(sql, id);
            return rowsAffected > 0;
        }

        public async Task<bool> DeleteByNameAsync(string name)
        {
                    var sql = @"
            DELETE FROM Companies 
            WHERE Name = {0}";

            var rowsAffected = await _context.Database.ExecuteSqlRawAsync(sql, name);
            return rowsAffected > 0;
        }
    }
}

