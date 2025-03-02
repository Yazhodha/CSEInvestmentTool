using CSEInvestmentTool.Application.Interfaces;
using CSEInvestmentTool.Domain.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CSEInvestmentTool.Application.Services;

public interface IInvestmentAllocationService
{
    Task<decimal> GetMonthlyInvestmentAmountAsync();
    Task<bool> UpdateMonthlyInvestmentAmountAsync(decimal amount);
    Task<List<InvestmentRecommendation>> CalculateInvestmentAllocationsAsync(
        List<StockScore> rankedStocks,
        DateTime recommendationDate,
        decimal? monthlyInvestmentAmount = null);
}

public class InvestmentAllocationService : IInvestmentAllocationService
{
    private readonly ILogger<InvestmentAllocationService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IAppSettingsRepository _settingsRepository;

    // Configuration constants with default values
    private readonly decimal _defaultMonthlyInvestmentAmount;
    private readonly int _maxStocks;
    private readonly decimal _minimumAllocation;
    private readonly decimal _highScoreThreshold;

    public InvestmentAllocationService(
        ILogger<InvestmentAllocationService> logger,
        IConfiguration configuration,
        IAppSettingsRepository settingsRepository)
    {
        _logger = logger;
        _configuration = configuration;
        _settingsRepository = settingsRepository;

        // Load configuration with defaults for other values
        _defaultMonthlyInvestmentAmount = _configuration.GetValue<decimal>("Investment:MonthlyAmount", 50000m);
        _maxStocks = _configuration.GetValue<int>("Investment:MaxStocks", 5);
        _minimumAllocation = _configuration.GetValue<decimal>("Investment:MinimumAllocation", 5000m);
        _highScoreThreshold = _configuration.GetValue<decimal>("Investment:HighScoreThreshold", 80m);

        _logger.LogInformation("Investment Allocation Service initialized with default values: " +
                             "Default Monthly Amount: {MonthlyAmount}, " +
                             "Max Stocks: {MaxStocks}, Minimum Allocation: {MinAllocation}",
            _defaultMonthlyInvestmentAmount, _maxStocks, _minimumAllocation);
    }

    public async Task<decimal> GetMonthlyInvestmentAmountAsync()
    {
        // Try to get the value from the database
        var amount = await _settingsRepository.GetSettingValueAsync<decimal>("MonthlyInvestmentAmount", _defaultMonthlyInvestmentAmount);
        return amount;
    }

    public async Task<bool> UpdateMonthlyInvestmentAmountAsync(decimal amount)
    {
        if (amount <= 0)
        {
            _logger.LogWarning("Attempted to set invalid monthly investment amount: {Amount}", amount);
            return false;
        }

        var result = await _settingsRepository.UpdateSettingAsync(
            "MonthlyInvestmentAmount",
            amount.ToString(),
            "Monthly investment budget in LKR");

        if (result)
        {
            _logger.LogInformation("Monthly investment amount updated to {Amount}", amount);
        }
        else
        {
            _logger.LogError("Failed to update monthly investment amount to {Amount}", amount);
        }

        return result;
    }

    public async Task<List<InvestmentRecommendation>> CalculateInvestmentAllocationsAsync(
        List<StockScore> rankedStocks,
        DateTime recommendationDate,
        decimal? monthlyInvestmentAmount = null)
    {
        try
        {
            // Use the provided amount or get from database
            decimal investmentAmount = monthlyInvestmentAmount ?? await GetMonthlyInvestmentAmountAsync();

            _logger.LogInformation("Calculating investment allocations for {Count} stocks on {Date} with budget {Amount:C}",
                rankedStocks.Count, recommendationDate, investmentAmount);

            var recommendations = new List<InvestmentRecommendation>();

            // Filter out scores of inactive stocks
            var activeStocks = rankedStocks.Where(s => s.Stock?.IsActive == true).ToList();

            if (activeStocks.Count == 0)
            {
                _logger.LogWarning("No active stocks provided for allocation calculation");
                return recommendations;
            }

            // Take top ranked stocks
            var topStocks = activeStocks
                .OrderByDescending(s => s.TotalScore)
                .Take(_maxStocks)
                .ToList();

            _logger.LogInformation("Selected top {Count} active stocks for investment", topStocks.Count);

            // Calculate allocation based on scores
            decimal totalScore = topStocks.Sum(s => s.TotalScore);
            decimal remainingAmount = investmentAmount;

            foreach (var stock in topStocks)
            {
                var recommendation = CalculateStockAllocation(stock, totalScore, investmentAmount, ref remainingAmount, recommendationDate);
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
        decimal investmentAmount,
        ref decimal remainingAmount,
        DateTime recommendationDate)
    {
        // Calculate proportional allocation
        decimal proportion = stock.TotalScore / totalScore;
        decimal recommendedAmount = Math.Round(
            investmentAmount * proportion,
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
            reasons.Add("Good NAV to price ratio");

        if (!reasons.Any())
            reasons.Add("Overall balanced performance");

        var reason = string.Join(". ", reasons) + ".";
        _logger.LogDebug("Generated recommendation reason for stock {StockId}: {Reason}",
            score.StockId, reason);

        return reason;
    }
}