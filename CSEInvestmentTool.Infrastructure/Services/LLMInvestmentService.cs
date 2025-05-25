using CSEInvestmentTool.Application.Interfaces;
using CSEInvestmentTool.Application.Models;
using CSEInvestmentTool.Application.Models.LLM;
using CSEInvestmentTool.Domain.Constants;
using CSEInvestmentTool.Domain.Models;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace CSEInvestmentTool.Infrastructure.Services;

public class LLMInvestmentService : ILLMInvestmentService
{
    private readonly ILLMProviderFactory _llmProviderFactory;
    private readonly ILogger<LLMInvestmentService> _logger;

    // Pre-defined investment philosophies
    private readonly Dictionary<InvestmentPhilosophyType, InvestmentPhilosophy> _philosophies;

    public LLMInvestmentService(
        ILLMProviderFactory llmProviderFactory,
        ILogger<LLMInvestmentService> logger)
    {
        _llmProviderFactory = llmProviderFactory;
        _logger = logger;

        // Initialize pre-defined philosophies
        _philosophies = InitializePhilosophies();
    }

    public async Task<List<InvestmentPhilosophy>> GetAvailablePhilosophiesAsync()
    {
        return _philosophies.Values.ToList();
    }

    public async Task<InvestmentPhilosophy?> GetPhilosophyAsync(InvestmentPhilosophyType philosophyType)
    {
        _philosophies.TryGetValue(philosophyType, out var philosophy);
        return philosophy;
    }

    public async Task<LLMInvestmentResponse> GenerateRecommendationsAsync(LLMInvestmentRequest request)
    {
        try
        {
            _logger.LogInformation("Generating LLM investment recommendations using {Philosophy} philosophy", request.Philosophy);

            var philosophy = await GetPhilosophyAsync(request.Philosophy);
            if (philosophy == null)
            {
                return new LLMInvestmentResponse
                {
                    Success = false,
                    ErrorMessage = $"Philosophy {request.Philosophy} not found"
                };
            }

            // Get the LLM provider
            var llmProvider = _llmProviderFactory.GetDefaultProvider();

            // Check if provider is available
            if (!await llmProvider.IsAvailableAsync())
            {
                return new LLMInvestmentResponse
                {
                    Success = false,
                    ErrorMessage = $"LLM provider {llmProvider.ProviderName} is not available"
                };
            }

            // Build the prompts
            var (systemPrompt, userPrompt) = BuildInvestmentPrompts(request, philosophy);

            // Create LLM request
            var llmRequest = new LLMRequest
            {
                SystemPrompt = systemPrompt,
                UserPrompt = userPrompt,
                Temperature = 0.7m,
                MaxTokens = 2000
            };

            // Call LLM
            var llmResponse = await llmProvider.CompleteChatAsync(llmRequest);

            if (!llmResponse.Success)
            {
                return new LLMInvestmentResponse
                {
                    Success = false,
                    ErrorMessage = $"LLM call failed: {llmResponse.ErrorMessage}"
                };
            }

            // Parse the response
            var recommendations = ParseLLMResponse(llmResponse.Content, request.Stocks);

            return new LLMInvestmentResponse
            {
                Success = true,
                Recommendations = recommendations.Recommendations,
                OverallAnalysis = recommendations.OverallAnalysis,
                MarketOutlook = recommendations.MarketOutlook,
                RiskFactors = recommendations.RiskFactors,
                LLMProvider = llmProvider.ProviderName,
                Model = llmProvider.Model
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating LLM investment recommendations");
            return new LLMInvestmentResponse
            {
                Success = false,
                ErrorMessage = $"Failed to generate recommendations: {ex.Message}"
            };
        }
    }

    public async Task<List<InvestmentRecommendation>> ConvertToInvestmentRecommendationsAsync(
        LLMInvestmentResponse llmResponse,
        DateTime recommendationDate)
    {
        var recommendations = new List<InvestmentRecommendation>();

        foreach (var llmRec in llmResponse.Recommendations)
        {
            recommendations.Add(new InvestmentRecommendation
            {
                StockId = llmRec.StockId,
                RecommendationDate = recommendationDate,
                RecommendedAmount = llmRec.RecommendedAmount,
                RecommendationReason = $"{llmResponse.LLMProvider} Analysis: {llmRec.Reasoning}. Risk: {llmRec.RiskAssessment}. Confidence: {llmRec.ConfidenceScore}%",
                LastUpdated = DateTime.UtcNow
            });
        }

        return recommendations;
    }

    private Dictionary<InvestmentPhilosophyType, InvestmentPhilosophy> InitializePhilosophies()
    {
        return new Dictionary<InvestmentPhilosophyType, InvestmentPhilosophy>
        {
            {
                InvestmentPhilosophyType.ValueInvesting,
                new InvestmentPhilosophy
                {
                    Name = "Value Investing",
                    Description = "Focus on undervalued stocks with strong fundamentals, low P/E ratios, and good dividend yields",
                    PromptTemplate = "Analyze these stocks using Benjamin Graham's value investing principles. Look for undervalued companies with strong balance sheets, low debt, consistent earnings, and trading below intrinsic value."
                }
            },
            {
                InvestmentPhilosophyType.GrowthInvesting,
                new InvestmentPhilosophy
                {
                    Name = "Growth Investing",
                    Description = "Focus on companies with high growth potential and strong earnings growth",
                    PromptTemplate = "Analyze these stocks for growth potential. Focus on companies with strong revenue growth, expanding margins, innovative business models, and market leadership positions."
                }
            },
            {
                InvestmentPhilosophyType.DividendInvesting,
                new InvestmentPhilosophy
                {
                    Name = "Dividend Investing",
                    Description = "Focus on stable, dividend-paying stocks for regular income",
                    PromptTemplate = "Analyze these stocks for dividend investing. Prioritize companies with consistent dividend payments, good dividend yield, sustainable payout ratios, and stable cash flows."
                }
            },
            {
                InvestmentPhilosophyType.BalancedApproach,
                new InvestmentPhilosophy
                {
                    Name = "Balanced Approach",
                    Description = "Mix of value and growth stocks for balanced risk-return profile",
                    PromptTemplate = "Analyze these stocks using a balanced approach combining value and growth principles. Look for companies with reasonable valuations, growth prospects, and financial stability."
                }
            },
            {
                InvestmentPhilosophyType.Conservative,
                new InvestmentPhilosophy
                {
                    Name = "Conservative",
                    Description = "Focus on low-risk, stable companies with strong balance sheets",
                    PromptTemplate = "Analyze these stocks with a conservative approach. Prioritize financial stability, low debt levels, consistent earnings, and established market positions. Minimize risk over maximizing returns."
                }
            }
        };
    }

    private (string SystemPrompt, string UserPrompt) BuildInvestmentPrompts(LLMInvestmentRequest request, InvestmentPhilosophy philosophy)
    {
        var systemPrompt = $@"You are an expert investment advisor specializing in {philosophy.Name} for the Sri Lankan stock market.

Philosophy: {philosophy.Description}
Analysis Instructions: {philosophy.PromptTemplate}

You must respond with valid JSON in the following format:
{{
  ""overallAnalysis"": ""Brief overall market/portfolio analysis"",
  ""marketOutlook"": ""Current market outlook and trends"",
  ""riskFactors"": [""risk factor 1"", ""risk factor 2""],
  ""recommendations"": [
    {{
      ""symbol"": ""STOCK_SYMBOL"",
      ""recommendedAmount"": 0,
      ""rank"": 1,
      ""reasoning"": ""Detailed reasoning for recommendation"",
      ""riskAssessment"": ""Risk analysis for this stock"",
      ""confidenceScore"": 85,
      ""strengthFactors"": [""strength 1"", ""strength 2""],
      ""concernFactors"": [""concern 1"", ""concern 2""]
    }}
  ]
}}

Ensure all recommendations sum to the total budget and rank them by preference.";

        var userPrompt = new StringBuilder();
        userPrompt.AppendLine($"Monthly Investment Budget: LKR {request.MonthlyBudget:N0}");
        userPrompt.AppendLine("Please analyze the following Sri Lankan stocks and provide investment recommendations:");
        userPrompt.AppendLine();

        foreach (var stock in request.Stocks)
        {
            userPrompt.AppendLine($"**{stock.Symbol} - {stock.CompanyName}**");
            userPrompt.AppendLine($"Sector: {stock.Sector}");
            userPrompt.AppendLine($"Market Price: LKR {stock.MarketPrice:N2}");
            userPrompt.AppendLine($"NAV: LKR {stock.NAV:N2}");
            userPrompt.AppendLine($"EPS: LKR {stock.EPS:N2}");
            userPrompt.AppendLine($"Annual Dividend: LKR {stock.AnnualDividend:N2}");
            userPrompt.AppendLine($"P/E Ratio: {stock.PERatio:N2}");
            userPrompt.AppendLine($"ROE: {stock.ROE:N2}%");
            userPrompt.AppendLine($"Dividend Yield: {stock.DividendYield:N2}%");
            userPrompt.AppendLine($"Debt/Equity: {stock.DebtToEquityRatio:N2}");
            userPrompt.AppendLine($"P/BV Ratio: {stock.PBV:N2}");
            userPrompt.AppendLine($"Current Algorithm Score: {stock.CurrentAlgorithmScore:N1}/100");
            userPrompt.AppendLine();
        }

        if (!string.IsNullOrEmpty(request.AdditionalInstructions))
        {
            userPrompt.AppendLine($"Additional Instructions: {request.AdditionalInstructions}");
            userPrompt.AppendLine();
        }

        userPrompt.AppendLine("Please provide your analysis and recommendations in the specified JSON format.");

        return (systemPrompt, userPrompt.ToString());
    }

    private LLMInvestmentResponse ParseLLMResponse(string responseContent, List<StockDataForAnalysis> stocks)
    {
        try
        {
            // Try to extract JSON from the response
            var jsonStart = responseContent.IndexOf('{');
            var jsonEnd = responseContent.LastIndexOf('}');

            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var jsonContent = responseContent.Substring(jsonStart, jsonEnd - jsonStart + 1);
                var llmAnalysis = JsonSerializer.Deserialize<LLMAnalysisResponse>(jsonContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (llmAnalysis != null)
                {
                    var recommendations = new List<LLMRecommendation>();
                    foreach (var rec in llmAnalysis.Recommendations)
                    {
                        var stock = stocks.FirstOrDefault(s => s.Symbol.Equals(rec.Symbol, StringComparison.OrdinalIgnoreCase));
                        if (stock != null)
                        {
                            recommendations.Add(new LLMRecommendation
                            {
                                StockId = stock.StockId,
                                Symbol = stock.Symbol,
                                CompanyName = stock.CompanyName,
                                RecommendedAmount = rec.RecommendedAmount,
                                RecommendationRank = rec.Rank,
                                Reasoning = rec.Reasoning,
                                RiskAssessment = rec.RiskAssessment,
                                ConfidenceScore = rec.ConfidenceScore,
                                StrengthFactors = rec.StrengthFactors,
                                ConcernFactors = rec.ConcernFactors
                            });
                        }
                    }

                    return new LLMInvestmentResponse
                    {
                        Success = true,
                        Recommendations = recommendations,
                        OverallAnalysis = llmAnalysis.OverallAnalysis,
                        MarketOutlook = llmAnalysis.MarketOutlook,
                        RiskFactors = llmAnalysis.RiskFactors
                    };
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse LLM JSON response, using fallback parsing");
        }

        // Fallback: create a simple response
        return new LLMInvestmentResponse
        {
            Success = true,
            OverallAnalysis = "LLM analysis completed but response parsing encountered issues. Please check logs for details.",
            Recommendations = new List<LLMRecommendation>()
        };
    }

    // Helper classes for parsing LLM response
    private class LLMAnalysisResponse
    {
        public string OverallAnalysis { get; set; } = "";
        public string MarketOutlook { get; set; } = "";
        public List<string> RiskFactors { get; set; } = new();
        public List<LLMRecommendationResponse> Recommendations { get; set; } = new();
    }

    private class LLMRecommendationResponse
    {
        public string Symbol { get; set; } = "";
        public decimal RecommendedAmount { get; set; }
        public int Rank { get; set; }
        public string Reasoning { get; set; } = "";
        public string RiskAssessment { get; set; } = "";
        public decimal ConfidenceScore { get; set; }
        public List<string> StrengthFactors { get; set; } = new();
        public List<string> ConcernFactors { get; set; } = new();
    }
}