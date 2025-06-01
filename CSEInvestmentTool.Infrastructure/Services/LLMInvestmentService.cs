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

            // Add this logging to see what we're sending
            _logger.LogInformation("=== INVESTMENT ANALYSIS PROMPT DEBUG ===");
            _logger.LogInformation("System Prompt Length: {Length} characters", systemPrompt.Length);
            _logger.LogInformation("System Prompt: {SystemPrompt}", systemPrompt);
            _logger.LogInformation("User Prompt Length: {Length} characters", userPrompt.Length);
            _logger.LogInformation("User Prompt: {UserPrompt}", userPrompt);
            _logger.LogInformation("=== END PROMPT DEBUG ===");

            // Create LLM request
            var llmRequest = new LLMRequest
            {
                SystemPrompt = systemPrompt,
                UserPrompt = userPrompt,
                Temperature = 0.3m,
                MaxTokens = 1500 // Increased from 800 to handle reasoning model
            };

            // Call LLM
            var llmResponse = await llmProvider.CompleteChatAsync(llmRequest);

            // Add this logging to see what we get back
            _logger.LogInformation("=== INVESTMENT ANALYSIS RESPONSE DEBUG ===");
            _logger.LogInformation("LLM Response Success: {Success}", llmResponse.Success);
            _logger.LogInformation("LLM Response Content Length: {Length}", llmResponse.Content?.Length ?? 0);
            _logger.LogInformation("LLM Response Content: '{Content}'", llmResponse.Content ?? "NULL");
            _logger.LogInformation("LLM Response Error: {Error}", llmResponse.ErrorMessage ?? "None");
            _logger.LogInformation("=== END RESPONSE DEBUG ===");

            if (!llmResponse.Success)
            {
                _logger.LogError("LLM analysis failed: {Error}", llmResponse.ErrorMessage);
                return new LLMInvestmentResponse
                {
                    Success = false,
                    ErrorMessage = $"LLM analysis failed: {llmResponse.ErrorMessage}"
                };
            }

            // Log token usage for monitoring
            if (llmResponse.TokensUsed.HasValue)
            {
                _logger.LogInformation("LLM analysis completed. Tokens used: {TokensUsed}", llmResponse.TokensUsed.Value);

                if (llmResponse.AdditionalData.TryGetValue("prompt_tokens", out var promptTokens) &&
                    llmResponse.AdditionalData.TryGetValue("completion_tokens", out var completionTokens))
                {
                    _logger.LogInformation("Token breakdown - Input: {InputTokens}, Output: {OutputTokens}",
                        promptTokens, completionTokens);
                }
            }

            // Parse the response
            var recommendations = ParseLLMResponse(llmResponse.Content, request.Stocks);

            return new LLMInvestmentResponse
            {
                Success = recommendations.Success,
                Recommendations = recommendations.Recommendations,
                OverallAnalysis = recommendations.OverallAnalysis,
                MarketOutlook = recommendations.MarketOutlook,
                RiskFactors = recommendations.RiskFactors,
                ErrorMessage = recommendations.ErrorMessage,
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
        var systemPrompt = $@"You are an expert investment advisor specializing in {philosophy.Name} for Sri Lankan stocks.

{philosophy.PromptTemplate}

CRITICAL INSTRUCTIONS:
- You MUST respond with ONLY valid JSON
- NO explanations, NO reasoning text, NO markdown
- Start your response with {{ and end with }}
- Follow this EXACT format:

{{
  ""overallAnalysis"": ""Brief market summary"",
  ""marketOutlook"": ""Short outlook"",
  ""riskFactors"": [""risk1"", ""risk2""],
  ""recommendations"": [
    {{
      ""symbol"": ""EXACT_STOCK_SYMBOL"",
      ""recommendedAmount"": 50000,
      ""rank"": 1,
      ""reasoning"": ""Short reason"",
      ""riskAssessment"": ""Brief risk"",
      ""confidenceScore"": 85
    }}
  ]
}}

Budget: LKR {request.MonthlyBudget:N0}. Allocate to max 5 best stocks. Ensure recommendedAmount values sum to approximately the budget.";

        var userPrompt = new StringBuilder();
        userPrompt.AppendLine("STOCKS TO ANALYZE:");

        foreach (var stock in request.Stocks)
        {
            userPrompt.AppendLine($"{stock.Symbol}: Price={stock.MarketPrice:F0}, NAV={stock.NAV:F0}, P/E={stock.PERatio:F1}, ROE={stock.ROE:F1}%, DivYield={stock.DividendYield:F1}%, D/E={stock.DebtToEquityRatio:F2}");
        }

        if (!string.IsNullOrEmpty(request.AdditionalInstructions))
        {
            userPrompt.AppendLine($"\nADDITIONAL: {request.AdditionalInstructions}");
        }

        userPrompt.AppendLine("\nRespond with ONLY the JSON object. No other text.");

        return (systemPrompt, userPrompt.ToString());
    }

    private LLMInvestmentResponse ParseLLMResponse(string responseContent, List<StockDataForAnalysis> stocks)
    {
        try
        {
            _logger.LogInformation("Parsing LLM response: {ResponseLength} characters", responseContent.Length);
            _logger.LogDebug("Full LLM response: {ResponseContent}", responseContent);

            // Clean the response content
            var cleanedContent = responseContent.Trim();
            string? jsonContent = null;

            // Strategy 1: Look for JSON object boundaries
            var jsonStart = cleanedContent.IndexOf('{');
            var jsonEnd = cleanedContent.LastIndexOf('}');

            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                jsonContent = cleanedContent.Substring(jsonStart, jsonEnd - jsonStart + 1);
                _logger.LogDebug("Strategy 1 - Extracted JSON: {JsonContent}", jsonContent);
            }

            // Strategy 2: Look for ```json code blocks
            if (string.IsNullOrEmpty(jsonContent))
            {
                var patterns = new[] { "```json", "```JSON", "```" };
                foreach (var pattern in patterns)
                {
                    var blockStart = cleanedContent.IndexOf(pattern);
                    if (blockStart >= 0)
                    {
                        var contentStart = blockStart + pattern.Length;
                        var blockEnd = cleanedContent.IndexOf("```", contentStart);
                        if (blockEnd > contentStart)
                        {
                            jsonContent = cleanedContent.Substring(contentStart, blockEnd - contentStart).Trim();
                            _logger.LogDebug("Strategy 2 - Extracted JSON from {Pattern} block: {JsonContent}", pattern, jsonContent);
                            break;
                        }
                    }
                }
            }

            // Strategy 3: Try to extract JSON from mixed content using regex
            if (string.IsNullOrEmpty(jsonContent))
            {
                var jsonPattern = @"\{[^{}]*(?:\{[^{}]*\}[^{}]*)*\}";
                var matches = System.Text.RegularExpressions.Regex.Matches(cleanedContent, jsonPattern, System.Text.RegularExpressions.RegexOptions.Singleline);

                foreach (System.Text.RegularExpressions.Match match in matches)
                {
                    var candidate = match.Value;
                    if (candidate.Contains("recommendations") && candidate.Contains("overallAnalysis"))
                    {
                        jsonContent = candidate;
                        _logger.LogDebug("Strategy 3 - Extracted JSON via regex: {JsonContent}", jsonContent);
                        break;
                    }
                }
            }

            // Strategy 4: Create fallback JSON if we can extract stock recommendations
            if (string.IsNullOrEmpty(jsonContent))
            {
                _logger.LogWarning("No valid JSON found, attempting to create fallback response");

                // Try to extract stock symbols and create a basic response
                var fallbackRecommendations = new List<LLMRecommendation>();
                decimal budgetPerStock = 100000m / Math.Min(stocks.Count, 3); // Distribute among top 3 stocks

                var topStocks = stocks.OrderByDescending(s => s.CurrentAlgorithmScore ?? 0).Take(3).ToList();

                for (int i = 0; i < topStocks.Count; i++)
                {
                    var stock = topStocks[i];
                    fallbackRecommendations.Add(new LLMRecommendation
                    {
                        StockId = stock.StockId,
                        Symbol = stock.Symbol,
                        CompanyName = stock.CompanyName,
                        RecommendedAmount = Math.Round(budgetPerStock, 0),
                        RecommendationRank = i + 1,
                        Reasoning = "Selected based on algorithm score and fundamentals",
                        RiskAssessment = "Standard market risk",
                        ConfidenceScore = 70,
                        StrengthFactors = new List<string> { "Good fundamentals" },
                        ConcernFactors = new List<string> { "Market volatility" }
                    });
                }

                _logger.LogInformation("Created fallback response with {Count} recommendations", fallbackRecommendations.Count);

                return new LLMInvestmentResponse
                {
                    Success = true,
                    Recommendations = fallbackRecommendations,
                    OverallAnalysis = "Analysis completed with fallback due to response parsing issues",
                    MarketOutlook = "Mixed market conditions require careful stock selection",
                    RiskFactors = new List<string> { "Market volatility", "Economic uncertainty" }
                };
            }

            // Try to parse the extracted JSON
            if (!string.IsNullOrEmpty(jsonContent))
            {
                try
                {
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        AllowTrailingCommas = true,
                        ReadCommentHandling = JsonCommentHandling.Skip
                    };

                    var llmAnalysis = JsonSerializer.Deserialize<LLMAnalysisResponse>(jsonContent, options);

                    if (llmAnalysis?.Recommendations?.Any() == true)
                    {
                        var recommendations = new List<LLMRecommendation>();
                        foreach (var rec in llmAnalysis.Recommendations)
                        {
                            var stock = stocks.FirstOrDefault(s =>
                                s.Symbol.Equals(rec.Symbol, StringComparison.OrdinalIgnoreCase));

                            if (stock != null)
                            {
                                recommendations.Add(new LLMRecommendation
                                {
                                    StockId = stock.StockId,
                                    Symbol = stock.Symbol,
                                    CompanyName = stock.CompanyName,
                                    RecommendedAmount = rec.RecommendedAmount,
                                    RecommendationRank = rec.Rank,
                                    Reasoning = rec.Reasoning ?? "AI recommended this stock",
                                    RiskAssessment = rec.RiskAssessment ?? "Standard risk",
                                    ConfidenceScore = rec.ConfidenceScore,
                                    StrengthFactors = rec.StrengthFactors ?? new List<string>(),
                                    ConcernFactors = rec.ConcernFactors ?? new List<string>()
                                });
                            }
                            else
                            {
                                _logger.LogWarning("Stock symbol {Symbol} not found in provided stocks list", rec.Symbol);
                            }
                        }

                        if (recommendations.Any())
                        {
                            _logger.LogInformation("Successfully parsed {Count} recommendations from LLM response", recommendations.Count);

                            return new LLMInvestmentResponse
                            {
                                Success = true,
                                Recommendations = recommendations,
                                OverallAnalysis = llmAnalysis.OverallAnalysis ?? "Analysis completed",
                                MarketOutlook = llmAnalysis.MarketOutlook ?? "Market outlook available",
                                RiskFactors = llmAnalysis.RiskFactors ?? new List<string>()
                            };
                        }
                    }
                    else
                    {
                        _logger.LogWarning("LLM response parsed but contained no valid recommendations");
                    }
                }
                catch (JsonException jsonEx)
                {
                    _logger.LogError(jsonEx, "JSON parsing failed. JSON content: {JsonContent}", jsonContent);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse LLM response");
        }

        // Return error response
        return new LLMInvestmentResponse
        {
            Success = false,
            ErrorMessage = "Failed to parse LLM response. The AI may have generated an invalid format."
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