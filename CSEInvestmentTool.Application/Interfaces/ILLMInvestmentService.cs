using CSEInvestmentTool.Application.Models;
using CSEInvestmentTool.Domain.Constants;
using CSEInvestmentTool.Domain.Models;

namespace CSEInvestmentTool.Application.Interfaces;

public interface ILLMInvestmentService
{
    /// <summary>
    /// Generates investment recommendations using LLM analysis
    /// </summary>
    /// <param name="request">Investment analysis request with philosophy and stock data</param>
    /// <returns>LLM-based investment recommendations</returns>
    Task<LLMInvestmentResponse> GenerateRecommendationsAsync(LLMInvestmentRequest request);

    /// <summary>
    /// Gets available investment philosophies
    /// </summary>
    /// <returns>List of available investment philosophies</returns>
    Task<List<InvestmentPhilosophy>> GetAvailablePhilosophiesAsync();

    /// <summary>
    /// Gets a specific investment philosophy by type
    /// </summary>
    /// <param name="philosophyType">The philosophy type</param>
    /// <returns>Investment philosophy details</returns>
    Task<InvestmentPhilosophy?> GetPhilosophyAsync(InvestmentPhilosophyType philosophyType);

    /// <summary>
    /// Converts LLM recommendations to domain InvestmentRecommendation objects
    /// </summary>
    /// <param name="llmResponse">LLM response</param>
    /// <param name="recommendationDate">Date for the recommendations</param>
    /// <returns>List of investment recommendations</returns>
    Task<List<InvestmentRecommendation>> ConvertToInvestmentRecommendationsAsync(
        LLMInvestmentResponse llmResponse,
        DateTime recommendationDate);
}