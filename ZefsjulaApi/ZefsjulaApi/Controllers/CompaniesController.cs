using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZefsjulaApi.Exceptions;
using ZefsjulaApi.Models.DTO;
using ZefsjulaApi.Models.Responses;
using ZefsjulaApi.Services;

namespace ZefsjulaApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Consumes("application/json")]
    [Produces("application/json")]
    [Authorize] // Require authentication for all endpoints
    public class CompaniesController : ControllerBase
    {
        private readonly ICompanyService _companyService;

        public CompaniesController(ICompanyService companyService)
        {
            _companyService = companyService;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<IEnumerable<CompanyDto>>>> GetAllAsync()
        {
            var result = await _companyService.GetAllCompaniesAsync();
            return Ok(result);
        }

        [HttpGet("paged")]
        public async Task<ActionResult<PagedResponse<CompanyDto>>> GetPagedAsync(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            var result = await _companyService.GetPagedCompaniesAsync(pageNumber, pageSize);
            return Ok(result);
        }

        //[HttpGet("analytics")]
        //public async Task<ActionResult<AnalyticsResponse<IEnumerable<CompanyDto>>>> GetWithAnalyticsAsync()
        //{
        //    var result = await _companyService.GetCompaniesWithAnalyticsAsync();
        //    return Ok(result);
        //}

        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<CompanyDto>>> GetByIdAsync(int id)
        {
            var result = await _companyService.GetCompanyByIdAsync(id);
            return Ok(result);
        }

        [HttpGet("status/{status}")]
        public async Task<ActionResult<ApiResponse<IEnumerable<CompanyDto>>>> GetByStatusAsync(string status)
        {
            var result = await _companyService.GetCompaniesByStatusAsync(status);
            return Ok(result);
        }

        [HttpGet("country/{countryCode}")]
        public async Task<ActionResult<ApiResponse<IEnumerable<CompanyDto>>>> GetByCountryAsync(string countryCode)
        {
            var result = await _companyService.GetCompaniesByCountryAsync(countryCode);
            return Ok(result);
        }

        [HttpGet("funding")]
        public async Task<ActionResult<ApiResponse<IEnumerable<CompanyDto>>>> GetByFundingRangeAsync(
            [FromQuery] decimal minFunding = 0,
            [FromQuery] decimal maxFunding = decimal.MaxValue)
        {
            var result = await _companyService.GetCompaniesByFundingRangeAsync(minFunding, maxFunding);
            return Ok(result);
        }


        [HttpPost]
        [Authorize(Policy = "ManagerOrAdmin")] // Only Managers and Admins can create companies
        public async Task<ActionResult<ApiResponse<CompanyDto>>> CreateAsync([FromBody] CreateCompanyDto createCompanyDto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.ToDictionary(
                       kvp => kvp.Key,
                       kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>()
                );

                throw new ValidationException(errors);
            }

            var result = await _companyService.CreateCompanyAsync(createCompanyDto);
            return Created($"/api/companies/{result.Data?.CompanyId}", result);
        }



        [HttpPut("{id}")]
        [Authorize(Policy = "ManagerOrAdmin")] // Only Managers and Admins can update companies
        public async Task<ActionResult<ApiResponse<CompanyDto>>> UpdateByIdAsync([FromRoute] int id, [FromBody] UpdateCompanyDto updateCompanyDto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>()
                );
                throw new ValidationException(errors);
            }

            var result = await _companyService.UpdateCompanyByIdAsync(id, updateCompanyDto);
            return Ok(result);
        }

        [HttpPut("name/{name}")]
        [Authorize(Policy = "ManagerOrAdmin")] // Only Managers and Admins can update companies
        public async Task<ActionResult<ApiResponse<CompanyDto>>> UpdateByNameAsync([FromRoute] string name, [FromBody] UpdateCompanyDto updateCompanyDto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>()
                );
                throw new ValidationException(errors);
            }

            var result = await _companyService.UpdateCompanyByNameAsync(name, updateCompanyDto);
            return Ok(result);
        }


        [HttpDelete("{id}")]
        [Authorize(Policy = "AdminOnly")] // Only Admins can delete companies
        public async Task<ActionResult<ApiResponse<string>>> DeleteByIdAsync([FromRoute] int id)
        {
            var result = await _companyService.DeleteCompanyByIdAsync(id);
            return Ok(result);
        }

        [HttpDelete("name/{name}")]
        [Authorize(Policy = "AdminOnly")] // Only Admins can delete companies
        public async Task<ActionResult<ApiResponse<string>>> DeleteByNameAsync([FromRoute] string name)
        {
            var result = await _companyService.DeleteCompanyByNameAsync(name);
            return Ok(result);
        }

        // Frontend-friendly endpoints for React dashboard
        [HttpGet("search")]
        [AllowAnonymous] // Allow public access for search
        public async Task<ActionResult<PagedResponse<CompanyDto>>> SearchAsync(
            [FromQuery] string? query = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            // Simple search implementation - you can enhance this
            if (string.IsNullOrEmpty(query))
            {
                var result = await _companyService.GetPagedCompaniesAsync(pageNumber, pageSize);
                return Ok(result);
            }
            
            // For now, return all companies - you can add search logic later
            var searchResult = await _companyService.GetPagedCompaniesAsync(pageNumber, pageSize);
            return Ok(searchResult);
        }

        [HttpGet("export")]
        [AllowAnonymous] // Allow public access for AI data export
        public async Task<ActionResult<ApiResponse<IEnumerable<CompanyDto>>>> ExportAsync(
            [FromQuery] int limit = 1000)
        {
            // Return companies for AI processing
            var result = await _companyService.GetAllCompaniesAsync();
            return Ok(result);
        }

    }

}