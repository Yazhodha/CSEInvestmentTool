using CSEInvestmentTool.Domain.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace CSEInvestmentTool.Application.Services;

public interface IInvestmentAllocationService
{
    List<InvestmentRecommendation> CalculateInvestmentAllocations(
        List<StockScore> rankedStocks,
        DateTime recommendationDate);
}

public class InvestmentAllocationService : IInvestmentAllocationService
{
    private readonly ILogger<InvestmentAllocationService> _logger;
    private readonly IConfiguration _configuration;
    
    // Configuration constants with default values
    private readonly decimal _monthlyInvestmentAmount;
    private readonly int _maxStocks;
    private readonly decimal _minimumAllocation;
    private readonly decimal _highScoreThreshold;

    public InvestmentAllocationService(
        ILogger<InvestmentAllocationService> logger,
        IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;

        // Load configuration with defaults
        _monthlyInvestmentAmount = _configuration.GetValue<decimal>("Investment:MonthlyAmount", 50000m);
        _maxStocks = _configuration.GetValue<int>("Investment:MaxStocks", 5);
        _minimumAllocation = _configuration.GetValue<decimal>("Investment:MinimumAllocation", 5000m);
        _highScoreThreshold = _configuration.GetValue<decimal>("Investment:HighScoreThreshold", 80m);

        _logger.LogInformation("Investment Allocation Service initialized with: Monthly Amount: {MonthlyAmount}, " +
                             "Max Stocks: {MaxStocks}, Minimum Allocation: {MinAllocation}",
            _monthlyInvestmentAmount, _maxStocks, _minimumAllocation);
    }

    public List<InvestmentRecommendation> CalculateInvestmentAllocations(
        List<StockScore> rankedStocks,
        DateTime recommendationDate)
    {
        try
        {
            _logger.LogInformation("Calculating investment allocations for {Count} stocks on {Date}",
                rankedStocks.Count, recommendationDate);

            var recommendations = new List<InvestmentRecommendation>();

            if (!rankedStocks.Any())
            {
                _logger.LogWarning("No stocks provided for allocation calculation");
                return recommendations;
            }

            // Take top ranked stocks
            var topStocks = rankedStocks
                .OrderByDescending(s => s.TotalScore)
                .Take(_maxStocks)
                .ToList();

            _logger.LogInformation("Selected top {Count} stocks for investment", topStocks.Count);

            // Calculate allocation based on scores
            decimal totalScore = topStocks.Sum(s => s.TotalScore);
            decimal remainingAmount = _monthlyInvestmentAmount;

            foreach (var stock in topStocks)
            {
                var recommendation = CalculateStockAllocation(stock, totalScore, ref remainingAmount, recommendationDate);
                recommendations.Add(recommendation);

                _logger.LogDebug("Allocated {Amount:C} to stock {StockId}. Remaining amount: {Remaining:C}",
                    recommendation.RecommendedAmount, stock.StockId, remainingAmount);
            }

            // Distribute any remaining amount
            if (remainingAmount > 0 && recommendations.Any())
            {
                recommendations[0].RecommendedAmount += remainingAmount;
                _logger.LogInformation("Distributed remaining amount {Amount:C} to top stock",
                    remainingAmount);
            }

            return recommendations;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating investment allocations");
            throw;
        }
    }

    private InvestmentRecommendation CalculateStockAllocation(
        StockScore stock,
        decimal totalScore,
        ref decimal remainingAmount,
        DateTime recommendationDate)
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
            _logger.LogDebug("Adjusting allocation for stock {StockId} to minimum amount", stock.StockId);
            recommendedAmount = _minimumAllocation;
        }

        // Adjust for remaining amount
        if (recommendedAmount > remainingAmount)
        {
            _logger.LogDebug("Adjusting allocation for stock {StockId} to remaining amount", stock.StockId);
            recommendedAmount = remainingAmount;
        }

        remainingAmount -= recommendedAmount;

        return new InvestmentRecommendation
        {
            StockId = stock.StockId,
            RecommendationDate = recommendationDate,
            RecommendedAmount = recommendedAmount,
            RecommendationReason = GenerateRecommendationReason(stock),
            LastUpdated = DateTime.UtcNow
        };
    }

    private string GenerateRecommendationReason(StockScore score)
    {
        var reasons = new List<string>();

        if (score.PEScore >= _highScoreThreshold)
            reasons.Add("Attractive P/E ratio");
        if (score.ROEScore >= _highScoreThreshold)
            reasons.Add("Strong return on equity");
        if (score.DividendYieldScore >= _highScoreThreshold)
            reasons.Add("High dividend yield");
        if (score.DebtEquityScore >= _highScoreThreshold)
            reasons.Add("Healthy debt levels");
        if (score.ProfitMarginScore >= _highScoreThreshold)
            reasons.Add("Good profit margins");

        if (!reasons.Any())
            reasons.Add("Overall balanced performance");

        var reason = string.Join(". ", reasons) + ".";
        _logger.LogDebug("Generated recommendation reason for stock {StockId}: {Reason}",
            score.StockId, reason);

        return reason;
    }
}