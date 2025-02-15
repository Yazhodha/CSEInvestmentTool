using CSEInvestmentTool.Domain.Models;

namespace CSEInvestmentTool.Application.Interfaces;

public interface IInvestmentRecommendationRepository
{
    Task<IEnumerable<InvestmentRecommendation>> GetLatestRecommendationsAsync();
    Task AddRecommendationAsync(InvestmentRecommendation recommendation);
}
