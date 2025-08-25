using Microsoft.ML;
using Microsoft.ML.Data;
using ZefsjulaApi.Models;
using ZefsjulaApi.Models.AI;
using ZefsjulaApi.Models.DTO;
using ZefsjulaApi.Services.AI_Interface;
using ZefsjulaApi.Data;
using Microsoft.EntityFrameworkCore;

namespace ZefsjulaApi.Services.AI_IMple
{
    public class EmbeddingService : IEmbeddingService
    {
        private readonly MLContext _mlContext;
        private readonly StartupDbContext _dbContext;
        private readonly ILogger<EmbeddingService> _logger;
        private ITransformer? _model;
        private readonly object _modelLock = new();

        public EmbeddingService(StartupDbContext dbContext, ILogger<EmbeddingService> logger)
        {
            _mlContext = new MLContext(seed: 42);
            _dbContext = dbContext;
            _logger = logger;

            // Initialize the model when the service is created
            _ = Task.Run(InitializeModelAsync);
        }

        private async Task InitializeModelAsync()
        {
            try
            {
                _logger.LogInformation("🔄 Initializing ML.NET embedding model with real company data...");

                // Get real company data using ONLY available fields
                var companies = await _dbContext.Companies
                    .Where(c => !string.IsNullOrEmpty(c.Name) &&
                               !string.IsNullOrEmpty(c.CategoryList))
                    .Take(1000) // Limit for performance
                    .Select(c => new TextData
                    {
                        // Build text from available fields only
                        Input = $"{c.Name} {c.CategoryList} {c.Status ?? ""} {c.City ?? ""} {c.CountryCode ?? ""}".Trim()
                    })
                    .ToArrayAsync();

                if (companies.Length == 0)
                {
                    _logger.LogWarning("⚠️ No company data found, using dummy data");
                    companies = GetDummyData();
                }

                _logger.LogInformation("📊 Training embedding model with {Count} companies", companies.Length);

                // Create enhanced text processing pipeline
                var pipeline = _mlContext.Transforms.Text.NormalizeText("NormalizedText", nameof(TextData.Input))
                    .Append(_mlContext.Transforms.Text.TokenizeIntoWords("Tokens", "NormalizedText"))
                    .Append(_mlContext.Transforms.Text.RemoveDefaultStopWords("FilteredTokens", "Tokens"))
                    .Append(_mlContext.Transforms.Text.FeaturizeText("Features", "FilteredTokens"));

                // Load data and fit the model
                var dataView = _mlContext.Data.LoadFromEnumerable(companies);

                lock (_modelLock)
                {
                    _model = pipeline.Fit(dataView);
                }

                _logger.LogInformation("✅ Embedding model initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to initialize embedding model");
            }
        }

        private TextData[] GetDummyData()
        {
            return new[]
            {
                new TextData { Input = "Apple technology smartphone operating Cupertino US" },
                new TextData { Input = "Microsoft software cloud computing acquired Seattle US" },
                new TextData { Input = "Tesla electric vehicles automotive operating Palo Alto US" },
                new TextData { Input = "Netflix streaming entertainment media operating Los Gatos US" },
                new TextData { Input = "Goldman Sachs financial services banking operating New York US" },
                new TextData { Input = "Uber transportation ridesharing technology operating San Francisco US" },
                new TextData { Input = "Airbnb hospitality travel accommodation operating San Francisco US" }
            };
        }

        public async Task<float[]> GenerateEmbeddingAsync(string text)
        {
            // Wait for model to be initialized
            while (_model == null)
            {
                await Task.Delay(100);
            }

            return await Task.Run(() =>
            {
                try
                {
                    var inputData = new[] { new TextData { Input = text } };
                    var dataView = _mlContext.Data.LoadFromEnumerable(inputData);

                    ITransformer model;
                    lock (_modelLock)
                    {
                        model = _model!;
                    }

                    var transformed = model.Transform(dataView);
                    var features = _mlContext.Data.CreateEnumerable<TextFeatures>(transformed, false).First();

                    return features.Features ?? Array.Empty<float>();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Failed to generate embedding for: {Text}", text);
                    return Array.Empty<float>();
                }
            });
        }

        public async Task<List<CompanyEmbedding>> GenerateCompanyEmbeddingsAsync(List<CompanyDto> companies)
        {
            var embeddings = new List<CompanyEmbedding>();

            foreach (var company in companies)
            {
                try
                {
                    // Create text representation using only available CompanyDto fields
                    var companyText = $"{company.Name} {company.CategoryList} {company.Status} {company.City} {company.CountryCode}".Trim();

                    // Generate embedding
                    var embedding = await GenerateEmbeddingAsync(companyText);

                    if (embedding.Length > 0)
                    {
                        embeddings.Add(new CompanyEmbedding
                        {
                            CompanyId = company.CompanyId,
                            CompanyName = company.Name,
                            Embedding = embedding,
                            ClusterId = 0, // Will be set by clustering service
                            SimilarityScore = 0.0 // Will be calculated when comparing
                        });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Failed to generate embedding for company {CompanyId}: {Name}",
                        company.CompanyId, company.Name);
                }
            }

            _logger.LogInformation("✅ Generated embeddings for {Count}/{Total} companies",
                embeddings.Count, companies.Count);

            return embeddings;
        }

        public async Task<double> CalculateSimilarityAsync(float[] embedding1, float[] embedding2)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (embedding1.Length != embedding2.Length || embedding1.Length == 0)
                    {
                        return 0.0;
                    }

                    // Calculate cosine similarity
                    var dotProduct = embedding1.Zip(embedding2, (a, b) => a * b).Sum();
                    var magnitude1 = Math.Sqrt(embedding1.Sum(a => a * a));
                    var magnitude2 = Math.Sqrt(embedding2.Sum(b => b * b));

                    if (magnitude1 == 0.0 || magnitude2 == 0.0)
                    {
                        return 0.0;
                    }

                    var similarity = dotProduct / (magnitude1 * magnitude2);

                    // Convert to 0-1 range (cosine similarity can be -1 to 1)
                    return (similarity + 1.0) / 2.0;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Failed to calculate similarity");
                    return 0.0;
                }
            });
        }
    }
}