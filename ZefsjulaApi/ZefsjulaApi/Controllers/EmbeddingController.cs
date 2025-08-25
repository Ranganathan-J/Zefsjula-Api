using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZefsjulaApi.Models;
using ZefsjulaApi.Models.AI;
using ZefsjulaApi.Models.DTO;
using ZefsjulaApi.Models.Responses;
using ZefsjulaApi.Services;
using ZefsjulaApi.Services.AI_Interface;

namespace ZefsjulaApi.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [AllowAnonymous] // For testing purposes
    public class EmbeddingController : ControllerBase
    {
        private readonly IEmbeddingService _embeddingService;
        private readonly ICompanyService _companyService;
        private readonly ILogger<EmbeddingController> _logger;

        public EmbeddingController(
            IEmbeddingService embeddingService,
            ICompanyService companyService,
            ILogger<EmbeddingController> logger)
        {
            _embeddingService = embeddingService;
            _companyService = companyService;
            _logger = logger;
        }

        /// <summary>
        /// Generate embedding for any text
        /// </summary>
        /// <param name="request">Text to convert to embedding</param>
        /// <returns>Vector representation of the text</returns>
        [HttpPost("generate")]
        public async Task<ActionResult<ApiResponse<object>>> GenerateEmbedding([FromBody] EmbeddingRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Text))
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Text cannot be empty",
                        Data = null
                    });
                }

                _logger.LogInformation("🧠 Generating embedding for: {Text}", request.Text);

                var embedding = await _embeddingService.GenerateEmbeddingAsync(request.Text);

                if (embedding.Length == 0)
                {
                    return StatusCode(500, new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Failed to generate embedding",
                        Data = null
                    });
                }

                var result = new
                {
                    InputText = request.Text,
                    EmbeddingLength = embedding.Length,
                    Embedding = request.ShowFullEmbedding ? embedding : embedding.Take(10).ToArray(),
                    FirstTenValues = embedding.Take(10).ToArray(),
                    ProcessedAt = DateTime.UtcNow,
                    Note = request.ShowFullEmbedding ? "Full embedding shown" : "Only first 10 values shown (use showFullEmbedding=true for all)"
                };

                _logger.LogInformation("✅ Generated {Length}-dimensional embedding", embedding.Length);

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = $"Generated {embedding.Length}-dimensional embedding",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to generate embedding");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Failed to generate embedding",
                    Data = null
                });
            }
        }

        /// <summary>
        /// Generate embeddings for multiple companies
        /// </summary>
        /// <param name="limit">Number of companies to process (max 50)</param>
        /// <returns>Company embeddings</returns>
        [HttpPost("companies")]
        public async Task<ActionResult<ApiResponse<List<CompanyEmbedding>>>> GenerateCompanyEmbeddings([FromQuery] int limit = 10)
        {
            try
            {
                if (limit < 1 || limit > 50)
                {
                    return BadRequest(new ApiResponse<List<CompanyEmbedding>>
                    {
                        Success = false,
                        Message = "Limit must be between 1 and 50",
                        Data = new List<CompanyEmbedding>()
                    });
                }

                _logger.LogInformation("🏢 Generating embeddings for {Limit} companies", limit);

                // Get all companies first, then take the limit
                var companiesResponse = await _companyService.GetAllCompaniesAsync();

                if (!companiesResponse.Success || companiesResponse.Data == null)
                {
                    return Ok(new ApiResponse<List<CompanyEmbedding>>
                    {
                        Success = true,
                        Message = "No companies found",
                        Data = new List<CompanyEmbedding>()
                    });
                }

                // Take the requested limit
                var companies = companiesResponse.Data.Take(limit).ToList();

                if (!companies.Any())
                {
                    return Ok(new ApiResponse<List<CompanyEmbedding>>
                    {
                        Success = true,
                        Message = "No companies found",
                        Data = new List<CompanyEmbedding>()
                    });
                }

                // Generate embeddings
                var embeddings = await _embeddingService.GenerateCompanyEmbeddingsAsync(companies);

                _logger.LogInformation("✅ Generated embeddings for {Count} companies", embeddings.Count);

                return Ok(new ApiResponse<List<CompanyEmbedding>>
                {
                    Success = true,
                    Message = $"Generated embeddings for {embeddings.Count} companies",
                    Data = embeddings
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to generate company embeddings");
                return StatusCode(500, new ApiResponse<List<CompanyEmbedding>>
                {
                    Success = false,
                    Message = "Failed to generate company embeddings",
                    Data = new List<CompanyEmbedding>()
                });
            }
        }

        /// <summary>
        /// Calculate similarity between two text inputs
        /// </summary>
        /// <param name="request">Two texts to compare</param>
        /// <returns>Similarity score between 0.0 and 1.0</returns>
        [HttpPost("similarity")]
        public async Task<ActionResult<ApiResponse<object>>> CalculateSimilarity([FromBody] SimilarityRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Text1) || string.IsNullOrWhiteSpace(request.Text2))
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Both texts must be provided",
                        Data = null
                    });
                }

                _logger.LogInformation("🔍 Calculating similarity between texts");

                // Generate embeddings for both texts
                var embedding1 = await _embeddingService.GenerateEmbeddingAsync(request.Text1);
                var embedding2 = await _embeddingService.GenerateEmbeddingAsync(request.Text2);

                if (embedding1.Length == 0 || embedding2.Length == 0)
                {
                    return StatusCode(500, new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Failed to generate embeddings",
                        Data = null
                    });
                }

                // Calculate similarity
                var similarity = await _embeddingService.CalculateSimilarityAsync(embedding1, embedding2);

                var result = new
                {
                    Text1 = request.Text1,
                    Text2 = request.Text2,
                    SimilarityScore = Math.Round(similarity, 4),
                    SimilarityPercentage = Math.Round(similarity * 100, 2),
                    Interpretation = GetSimilarityInterpretation(similarity),
                    EmbeddingLength1 = embedding1.Length,
                    EmbeddingLength2 = embedding2.Length,
                    ProcessedAt = DateTime.UtcNow
                };

                _logger.LogInformation("✅ Similarity calculated: {Similarity:F4}", similarity);

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = $"Similarity: {similarity:P2}",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to calculate similarity");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Failed to calculate similarity",
                    Data = null
                });
            }
        }

        /// <summary>
        /// Test endpoint with predefined company examples
        /// </summary>
        /// <returns>Embeddings for sample companies</returns>
        [HttpGet("test-samples")]
        public async Task<ActionResult<ApiResponse<object>>> TestWithSamples()
        {
            try
            {
                _logger.LogInformation("🧪 Testing embeddings with sample companies");

                var sampleTexts = new[]
                {
                    "Apple technology smartphone consumer electronics",
                    "Microsoft software cloud computing enterprise",
                    "Tesla electric vehicles automotive transportation",
                    "Netflix streaming entertainment media content",
                    "Goldman Sachs financial services investment banking"
                };

                var results = new List<object>();

                for (int i = 0; i < sampleTexts.Length; i++)
                {
                    var embedding = await _embeddingService.GenerateEmbeddingAsync(sampleTexts[i]);

                    results.Add(new
                    {
                        Index = i + 1,
                        Text = sampleTexts[i],
                        EmbeddingLength = embedding.Length,
                        FirstFiveValues = embedding.Take(5).ToArray(),
                        LastFiveValues = embedding.TakeLast(5).ToArray()
                    });
                }

                var response = new
                {
                    SampleEmbeddings = results,
                    ProcessedAt = DateTime.UtcNow,
                    Note = "This shows how different company types get different embeddings"
                };

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = $"Generated sample embeddings for {results.Count} companies",
                    Data = response
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to test samples");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Failed to test samples",
                    Data = null
                });
            }
        }

        #region Helper Methods

        private string GetSimilarityInterpretation(double similarity)
        {
            return similarity switch
            {
                >= 0.9 => "Very High Similarity",
                >= 0.7 => "High Similarity",
                >= 0.5 => "Moderate Similarity",
                >= 0.3 => "Low Similarity",
                _ => "Very Low Similarity"
            };
        }

        #endregion

        [HttpGet("similar/{companyId:int}")]
        public async Task<ActionResult<ApiResponse<object>>> FindSimilarCompanies(
    int companyId,
    [FromQuery] int limit = 5)
        {
            try
            {
                // Get the target company
                var targetCompanyResponse = await _companyService.GetCompanyByIdAsync(companyId);
                if (!targetCompanyResponse.Success || targetCompanyResponse.Data == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Company not found",
                        Data = null
                    });
                }

                var targetCompany = targetCompanyResponse.Data;

                // Generate embedding for target company
                var targetText = $"{targetCompany.Name} {targetCompany.CategoryList} {targetCompany.City} {targetCompany.CountryCode}";
                var targetEmbedding = await _embeddingService.GenerateEmbeddingAsync(targetText);

                // Get other companies and find similar ones
                var allCompaniesResponse = await _companyService.GetAllCompaniesAsync();
                var otherCompanies = allCompaniesResponse.Data?
                    .Where(c => c.CompanyId != companyId)
                    .Take(100) // Limit for performance
                    .ToList() ?? new List<CompanyDto>();

                var similarities = new List<object>();

                foreach (var company in otherCompanies)
                {
                    var companyText = $"{company.Name} {company.CategoryList} {company.City} {company.CountryCode}";
                    var companyEmbedding = await _embeddingService.GenerateEmbeddingAsync(companyText);
                    var similarity = await _embeddingService.CalculateSimilarityAsync(targetEmbedding, companyEmbedding);

                    similarities.Add(new
                    {
                        CompanyId = company.CompanyId,
                        Name = company.Name,
                        Industry = company.CategoryList,
                        Country = company.CountryCode,
                        SimilarityScore = Math.Round(similarity, 4),
                        SimilarityPercentage = Math.Round(similarity * 100, 2)
                    });
                }

                var topSimilar = similarities
                    .OrderByDescending(s => (double)s.GetType().GetProperty("SimilarityScore")!.GetValue(s)!)
                    .Take(limit)
                    .ToList();

                var result = new
                {
                    TargetCompany = new
                    {
                        CompanyId = targetCompany.CompanyId,
                        Name = targetCompany.Name,
                        Industry = targetCompany.CategoryList
                    },
                    SimilarCompanies = topSimilar,
                    ProcessedAt = DateTime.UtcNow
                };

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = $"Found {topSimilar.Count} similar companies",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to find similar companies");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Failed to find similar companies",
                    Data = null
                });
            }
        }
        /// <summary>
        /// Find companies similar to a company name or any description string
        /// </summary>
        /// <param name="searchText">Company name or description to find similar companies for</param>
        /// <param name="limit">Maximum number of similar companies to return (1-20)</param>
        /// <returns>List of similar companies with similarity scores</returns>
        [HttpGet("find-similar")]
        public async Task<ActionResult<ApiResponse<object>>> FindSimilarByText(
            [FromQuery] string searchText,
            [FromQuery] int limit = 5)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchText))
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Search text cannot be empty",
                        Data = null
                    });
                }

                // Validate limit
                limit = Math.Min(Math.Max(limit, 1), 20); // Between 1 and 20

                _logger.LogInformation("🔍 Finding companies similar to: '{SearchText}'", searchText);

                // Generate embedding for the search text
                var searchEmbedding = await _embeddingService.GenerateEmbeddingAsync(searchText);

                if (searchEmbedding.Length == 0)
                {
                    return StatusCode(500, new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Failed to generate embedding for search text",
                        Data = null
                    });
                }

                // Get all companies from database
                var allCompaniesResponse = await _companyService.GetAllCompaniesAsync();

                if (!allCompaniesResponse.Success || allCompaniesResponse.Data == null)
                {
                    return Ok(new ApiResponse<object>
                    {
                        Success = true,
                        Message = "No companies found in database",
                        Data = new
                        {
                            SearchText = searchText,
                            SimilarCompanies = new List<object>(),
                            ProcessedAt = DateTime.UtcNow
                        }
                    });
                }

                var companies = allCompaniesResponse.Data.Take(300).ToList(); // Limit for performance
                var similarities = new List<object>();

                _logger.LogInformation("📊 Analyzing {Count} companies for similarity", companies.Count);

                // Calculate similarity for each company
                foreach (var company in companies)
                {
                    try
                    {
                        // Create text representation of the company
                        var companyText = $"{company.Name} {company.CategoryList} {company.Status} {company.City} {company.CountryCode}".Trim();

                        // Generate embedding for this company
                        var companyEmbedding = await _embeddingService.GenerateEmbeddingAsync(companyText);

                        if (companyEmbedding.Length > 0)
                        {
                            // Calculate similarity score
                            var similarity = await _embeddingService.CalculateSimilarityAsync(searchEmbedding, companyEmbedding);

                            similarities.Add(new
                            {
                                CompanyName = company.Name,
                                Industry = company.CategoryList ?? "Not specified",
                                Status = company.Status ?? "Unknown",
                                Location = $"{company.City ?? ""} {company.CountryCode ?? ""}".Trim(),
                                FundingUSD = company.FundingTotalUsd?.ToString("N0") ?? "Not disclosed",
                                SimilarityScore = Math.Round(similarity, 4),
                                SimilarityPercentage = Math.Round(similarity * 100, 2),
                                MatchStrength = GetMatchStrength(similarity)
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "⚠️ Failed to process company: {CompanyName}", company.Name);
                        // Continue with next company
                    }
                }

                // Sort by similarity and take top results
                var topSimilar = similarities
                    .OrderByDescending(s => (double)s.GetType().GetProperty("SimilarityScore")!.GetValue(s)!)
                    .Take(limit)
                    .ToList();

                var result = new
                {
                    SearchQuery = searchText,
                    TotalCompaniesAnalyzed = similarities.Count,
                    ResultsReturned = topSimilar.Count,
                    TopSimilarCompanies = topSimilar,
                    ProcessedAt = DateTime.UtcNow,
                    Instructions = new
                    {
                        Usage = "Use ?searchText=YourQuery&limit=10",
                        Examples = new[]
                        {
                    "?searchText=Apple&limit=5",
                    "?searchText=artificial intelligence&limit=10",
                    "?searchText=electric vehicle manufacturer&limit=8"
                }
                    }
                };

                _logger.LogInformation("✅ Found {Count} similar companies for '{SearchText}'", topSimilar.Count, searchText);

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = $"Found {topSimilar.Count} companies similar to '{searchText}'",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to find similar companies for '{SearchText}'", searchText);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Failed to find similar companies",
                    Data = null
                });
            }
        }

        // Helper method for match strength interpretation
        private string GetMatchStrength(double similarity)
        {
            return similarity switch
            {
                >= 0.85 => "Extremely Similar - Direct Competitor",
                >= 0.70 => "Very Similar - Same Industry",
                >= 0.60 => "Highly Similar - Related Business",
                >= 0.50 => "Moderately Similar - Some Overlap",
                >= 0.40 => "Somewhat Similar - Distant Relation",
                _ => "Low Similarity - Different Business"
            };
        }
    }




    // Request/Response Models
    public class EmbeddingRequest
    {
        public string Text { get; set; } = string.Empty;
        public bool ShowFullEmbedding { get; set; } = false;
    }

    public class SimilarityRequest
    {
        public string Text1 { get; set; } = string.Empty;
        public string Text2 { get; set; } = string.Empty;
    }
}