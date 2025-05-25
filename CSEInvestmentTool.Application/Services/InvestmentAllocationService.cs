using CSEInvestmentTool.Application.Interfaces;
using CSEInvestmentTool.Application.Models;
using CSEInvestmentTool.Domain.Constants;
using CSEInvestmentTool.Domain.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CSEInvestmentTool.Application.Services;

public class InvestmentAllocationService : IInvestmentAllocationService
{
    private readonly ILogger<InvestmentAllocationService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IAppSettingsRepository _settingsRepository;
    private readonly ILLMInvestmentService _llmInvestmentService;
    private readonly IStockRepository _stockRepository;
    private readonly IFundamentalDataRepository _fundamentalDataRepository;
    private readonly IStockScoreRepository _stockScoreRepository;

    // Configuration constants with default values
    private readonly decimal _defaultMonthlyInvestmentAmount;
    private readonly int _maxStocks;
    private readonly decimal _minimumAllocation;
    private readonly decimal _highScoreThreshold;

    public InvestmentAllocationService(
        ILogger<InvestmentAllocationService> logger,
        IConfiguration configuration,
        IAppSettingsRepository settingsRepository,
        ILLMInvestmentService llmInvestmentService,
        IStockRepository stockRepository,
        IFundamentalDataRepository fundamentalDataRepository,
        IStockScoreRepository stockScoreRepository)
    {
        _logger = logger;
        _configuration = configuration;
        _settingsRepository = settingsRepository;
        _llmInvestmentService = llmInvestmentService;
        _stockRepository = stockRepository;
        _fundamentalDataRepository = fundamentalDataRepository;
        _stockScoreRepository = stockScoreRepository;

        // Load configuration with defaults
        _defaultMonthlyInvestmentAmount = _configuration.GetValue<decimal>("Investment:MonthlyAmount", 50000m);
        _maxStocks = _configuration.GetValue<int>("Investment:MaxStocks", 5);
        _minimumAllocation = _configuration.GetValue<decimal>("Investment:MinimumAllocation", 5000m);
        _highScoreThreshold = _configuration.GetValue<decimal>("Investment:HighScoreThreshold", 80m);
    }

    public async Task<decimal> GetMonthlyInvestmentAmountAsync()
    {
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

        return result;
    }

    // Existing algorithm-based method (unchanged)
    public async Task<List<InvestmentRecommendation>> CalculateInvestmentAllocationsAsync(
        List<StockScore> rankedStocks,
        DateTime recommendationDate,
        decimal? monthlyInvestmentAmount = null)
    {
        try
        {
            decimal investmentAmount = monthlyInvestmentAmount ?? await GetMonthlyInvestmentAmountAsync();

            _logger.LogInformation("Calculating ALGORITHM-based investment allocations for {Count} stocks with budget {Amount:C}",
                rankedStocks.Count, investmentAmount);

            var recommendations = new List<InvestmentRecommendation>();
            var activeStocks = rankedStocks.Where(s => s.Stock?.IsActive == true).ToList();

            if (activeStocks.Count == 0)
            {
                _logger.LogWarning("No active stocks provided for allocation calculation");
                return recommendations;
            }

            var topStocks = activeStocks
                .OrderByDescending(s => s.TotalScore)
                .Take(_maxStocks)
                .ToList();

            decimal totalScore = topStocks.Sum(s => s.TotalScore);
            decimal remainingAmount = investmentAmount;

            foreach (var stock in topStocks)
            {
                if (remainingAmount <= 0) break;

                var recommendation = CalculateStockAllocation(stock, totalScore, investmentAmount, ref remainingAmount, recommendationDate);
                recommendations.Add(recommendation);
            }

            // Distribute any remaining amount proportionally
            if (remainingAmount > 0 && recommendations.Any())
            {
                decimal totalCurrentAllocation = recommendations.Sum(r => r.RecommendedAmount);
                foreach (var recommendation in recommendations)
                {
                    decimal proportion = recommendation.RecommendedAmount / totalCurrentAllocation;
                    decimal additionalAmount = Math.Round(remainingAmount * proportion, 0, MidpointRounding.AwayFromZero);
                    recommendation.RecommendedAmount += additionalAmount;
                    remainingAmount -= additionalAmount;
                }

                if (remainingAmount > 0)
                {
                    recommendations[0].RecommendedAmount += remainingAmount;
                }
            }

            return recommendations;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating algorithm-based investment allocations");
            throw;
        }
    }

    // NEW: LLM-based recommendation method
    public async Task<List<InvestmentRecommendation>> GenerateLLMRecommendationsAsync(
        InvestmentPhilosophyType philosophy,
        DateTime recommendationDate,
        decimal? monthlyInvestmentAmount = null,
        string? additionalInstructions = null)
    {
        try
        {
            decimal investmentAmount = monthlyInvestmentAmount ?? await GetMonthlyInvestmentAmountAsync();

            _logger.LogInformation("Generating LLM-based investment recommendations using {Philosophy} philosophy with budget {Amount:C}",
                philosophy, investmentAmount);

            // Get all active stocks with their fundamental data and scores
            var stocks = await _stockRepository.GetAllStocksAsync();
            var activeStocks = stocks.Where(s => s.IsActive).ToList();

            if (!activeStocks.Any())
            {
                _logger.LogWarning("No active stocks found for LLM analysis");
                return new List<InvestmentRecommendation>();
            }

            // Prepare stock data for LLM analysis
            var stockDataList = new List<StockDataForAnalysis>();

            foreach (var stock in activeStocks)
            {
                var fundamentalData = await _fundamentalDataRepository.GetLatestFundamentalDataForStockAsync(stock.StockId);
                var score = await _stockScoreRepository.GetLatestScoreForStockAsync(stock.StockId);

                if (fundamentalData != null)
                {
                    stockDataList.Add(new StockDataForAnalysis
                    {
                        StockId = stock.StockId,
                        Symbol = stock.Symbol,
                        CompanyName = stock.CompanyName,
                        Sector = stock.Sector,
                        MarketPrice = fundamentalData.MarketPrice,
                        NAV = fundamentalData.NAV,
                        EPS = fundamentalData.EPS,
                        AnnualDividend = fundamentalData.AnnualDividend,
                        TotalLiabilities = fundamentalData.TotalLiabilities,
                        TotalEquity = fundamentalData.TotalEquity,
                        PERatio = fundamentalData.PERatio,
                        ROE = fundamentalData.ROE,
                        DividendYield = fundamentalData.DividendYield,
                        DebtToEquityRatio = fundamentalData.DebtToEquityRatio,
                        PBV = fundamentalData.PBV,
                        Return = fundamentalData.Return,
                        CurrentAlgorithmScore = score?.TotalScore
                    });
                }
            }

            if (!stockDataList.Any())
            {
                _logger.LogWarning("No stocks with fundamental data found for LLM analysis");
                return new List<InvestmentRecommendation>();
            }

            // Create LLM request
            var llmRequest = new LLMInvestmentRequest
            {
                Philosophy = philosophy,
                MonthlyBudget = investmentAmount,
                Stocks = stockDataList,
                AdditionalInstructions = additionalInstructions
            };

            // Get LLM recommendations
            var llmResponse = await _llmInvestmentService.GenerateRecommendationsAsync(llmRequest);

            if (!llmResponse.Success)
            {
                _logger.LogError("LLM analysis failed: {Error}", llmResponse.ErrorMessage);
                throw new InvalidOperationException($"LLM analysis failed: {llmResponse.ErrorMessage}");
            }

            // Convert to domain recommendations
            var recommendations = await _llmInvestmentService.ConvertToInvestmentRecommendationsAsync(llmResponse, recommendationDate);

            _logger.LogInformation("Successfully generated {Count} LLM-based recommendations", recommendations.Count);

            return recommendations;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating LLM-based investment recommendations");
            throw;
        }
    }

    public async Task<RecommendationMethod> GetLastRecommendationMethodAsync()
    {
        var methodString = await _settingsRepository.GetSettingValueAsync("LastRecommendationMethod", "Algorithm");
        return Enum.TryParse<RecommendationMethod>(methodString, out var method) ? method : RecommendationMethod.Algorithm;
    }

    public async Task<bool> SetRecommendationMethodAsync(RecommendationMethod method)
    {
        return await _settingsRepository.UpdateSettingAsync(
            "LastRecommendationMethod",
            method.ToString(),
            "Last used recommendation method (Algorithm or LLM)");
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
            recommendedAmount = _minimumAllocation;
        }

        // Adjust for remaining amount
        if (recommendedAmount > remainingAmount)
        {
            recommendedAmount = remainingAmount;
        }

        remainingAmount -= recommendedAmount;

        return new InvestmentRecommendation
        {
            StockId = stock.StockId,
            RecommendationDate = recommendationDate,
            RecommendedAmount = recommendedAmount,
            RecommendationReason = GenerateAlgorithmRecommendationReason(stock),
            LastUpdated = DateTime.UtcNow
        };
    }

    private string GenerateAlgorithmRecommendationReason(StockScore score)
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

        return "Algorithm Analysis: " + string.Join(". ", reasons) + ".";
    }
}