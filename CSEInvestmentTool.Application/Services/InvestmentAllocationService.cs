using CSEInvestmentTool.Domain.Models;

namespace CSEInvestmentTool.Application.Services;

public class InvestmentAllocationService
{
    private readonly decimal _monthlyInvestmentAmount = 50000m; // LKR
    private readonly int _maxStocks = 5; // Maximum number of stocks to recommend
    private readonly decimal _minimumAllocation = 5000m; // Minimum amount per stock

    public List<InvestmentRecommendation> CalculateInvestmentAllocations(
        List<StockScore> rankedStocks,
        DateTime recommendationDate)
    {
        var recommendations = new List<InvestmentRecommendation>();
        
        // Take top ranked stocks
        var topStocks = rankedStocks
            .OrderByDescending(s => s.TotalScore)
            .Take(_maxStocks)
            .ToList();

        if (!topStocks.Any()) return recommendations;

        // Calculate allocation based on scores
        decimal totalScore = topStocks.Sum(s => s.TotalScore);
        decimal remainingAmount = _monthlyInvestmentAmount;
        
        foreach (var stock in topStocks)
        {
            // Calculate proportional allocation
            decimal proportion = stock.TotalScore / totalScore;
            decimal recommendedAmount = Math.Round(
                _monthlyInvestmentAmount * proportion, 
                0, 
                MidpointRounding.AwayFromZero);

            // Ensure minimum allocation
            if (recommendedAmount < _minimumAllocation)
            {
                recommendedAmount = _minimumAllocation;
            }

            // Adjust for remaining amount
            if (recommendedAmount > remainingAmount)
            {
                recommendedAmount = remainingAmount;
            }

            remainingAmount -= recommendedAmount;

            // Create recommendation
            var recommendation = new InvestmentRecommendation
            {
                StockId = stock.StockId,
                RecommendationDate = recommendationDate,
                RecommendedAmount = recommendedAmount,
                RecommendationReason = GenerateRecommendationReason(stock),
                LastUpdated = DateTime.UtcNow
            };

            recommendations.Add(recommendation);
        }

        // Distribute any remaining amount
        if (remainingAmount > 0 && recommendations.Any())
        {
            recommendations[0].RecommendedAmount += remainingAmount;
        }

        return recommendations;
    }

    private string GenerateRecommendationReason(StockScore score)
    {
        var reasons = new List<string>();

        if (score.PEScore >= 80)
            reasons.Add("Attractive P/E ratio");
        if (score.ROEScore >= 80)
            reasons.Add("Strong return on equity");
        if (score.DividendYieldScore >= 80)
            reasons.Add("High dividend yield");
        if (score.DebtEquityScore >= 80)
            reasons.Add("Healthy debt levels");
        if (score.ProfitMarginScore >= 80)
            reasons.Add("Good profit margins");

        if (!reasons.Any())
            reasons.Add("Overall balanced performance");

        return string.Join(". ", reasons) + ".";
    }
}