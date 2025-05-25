using CSEInvestmentTool.Application.Models;
using CSEInvestmentTool.Domain.Constants;
using CSEInvestmentTool.Domain.Models;

namespace CSEInvestmentTool.Application.Services;

public interface IInvestmentAllocationService
{
    Task<decimal> GetMonthlyInvestmentAmountAsync();
    Task<bool> UpdateMonthlyInvestmentAmountAsync(decimal amount);

    /// <summary>
    /// Calculates investment allocations using the traditional algorithm-based approach
    /// </summary>
    Task<List<InvestmentRecommendation>> CalculateInvestmentAllocationsAsync(
        List<StockScore> rankedStocks,
        DateTime recommendationDate,
        decimal? monthlyInvestmentAmount = null);

    /// <summary>
    /// Generates investment recommendations using LLM analysis
    /// </summary>
    /// <param name="philosophy">Investment philosophy to apply</param>
    /// <param name="recommendationDate">Date for the recommendations</param>
    /// <param name="monthlyInvestmentAmount">Optional budget override</param>
    /// <param name="additionalInstructions">Optional additional instructions for LLM</param>
    /// <returns>LLM-based investment recommendations</returns>
    Task<List<InvestmentRecommendation>> GenerateLLMRecommendationsAsync(
        InvestmentPhilosophyType philosophy,
        DateTime recommendationDate,
        decimal? monthlyInvestmentAmount = null,
        string? additionalInstructions = null);

    /// <summary>
    /// Gets the last used recommendation method
    /// </summary>
    Task<RecommendationMethod> GetLastRecommendationMethodAsync();

    /// <summary>
    /// Sets the preferred recommendation method
    /// </summary>
    Task<bool> SetRecommendationMethodAsync(RecommendationMethod method);
}