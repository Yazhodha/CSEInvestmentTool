using CSEInvestmentTool.Domain.Models;
using Microsoft.Extensions.Logging;

namespace CSEInvestmentTool.Application.Services;

public interface IStockScoringService
{
    StockScore CalculateScore(FundamentalData data);
}

public class StockScoringService : IStockScoringService
{
    private readonly ILogger<StockScoringService> _logger;

    // Scoring weights
    private static readonly decimal PE_WEIGHT = 0.25m;
    private static readonly decimal ROE_WEIGHT = 0.25m;
    private static readonly decimal DIVIDEND_WEIGHT = 0.20m;
    private static readonly decimal DEBT_EQUITY_WEIGHT = 0.15m;
    private static readonly decimal NAV_PRICE_WEIGHT = 0.15m;  // Changed from profit margin

    public StockScoringService(ILogger<StockScoringService> logger)
    {
        _logger = logger;
    }

    public StockScore CalculateScore(FundamentalData data)
    {
        _logger.LogInformation("Calculating score for stock {StockId}", data.StockId);

        try
        {
            var score = new StockScore
            {
                StockId = data.StockId,
                ScoreDate = data.Date,
                LastUpdated = DateTime.UtcNow
            };

            // Calculate individual scores (0-100 scale)
            score.PEScore = CalculatePEScore(data.PERatio);
            score.ROEScore = CalculateROEScore(data.ROE);
            score.DividendYieldScore = CalculateDividendYieldScore(data.DividendYield);
            score.DebtEquityScore = CalculateDebtEquityScore(data.DebtToEquityRatio);
            score.ProfitMarginScore = CalculateNAVPriceScore(data.NAV, data.MarketPrice); // Changed to NAV/Price score

            // Calculate weighted total score
            score.TotalScore = CalculateWeightedScore(score);

            _logger.LogInformation("Score calculated successfully for stock {StockId}. Total Score: {TotalScore}",
                data.StockId, score.TotalScore);

            return score;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating score for stock {StockId}", data.StockId);
            throw;
        }
    }

    private decimal CalculateWeightedScore(StockScore score)
    {
        return (score.PEScore * PE_WEIGHT) +
               (score.ROEScore * ROE_WEIGHT) +
               (score.DividendYieldScore * DIVIDEND_WEIGHT) +
               (score.DebtEquityScore * DEBT_EQUITY_WEIGHT) +
               (score.ProfitMarginScore * NAV_PRICE_WEIGHT); // Using profit margin score field for NAV/Price
    }

    private decimal CalculatePEScore(decimal? peRatio)
    {
        if (!peRatio.HasValue || peRatio <= 0)
        {
            _logger.LogDebug("Invalid P/E ratio: {PERatio}", peRatio);
            return 0;
        }

        var score = peRatio.Value switch
        {
            < 5 => 70,
            <= 15 => 100 - ((peRatio.Value - 5) * 3),
            <= 25 => 70 - ((peRatio.Value - 15) * 3),
            _ => Math.Max(0, 40 - ((peRatio.Value - 25) * 2))
        };

        _logger.LogDebug("P/E Score calculated: {Score} for P/E ratio: {PERatio}", score, peRatio);
        return score;
    }

    private decimal CalculateROEScore(decimal? roe)
    {
        if (!roe.HasValue)
        {
            _logger.LogDebug("Invalid ROE: {ROE}", roe);
            return 0;
        }

        var score = roe.Value switch
        {
            < 0 => 0,
            <= 5 => roe.Value * 5,
            <= 15 => 25 + ((roe.Value - 5) * 5),
            <= 25 => 75 + ((roe.Value - 15) * 2.5m),
            _ => 100
        };

        _logger.LogDebug("ROE Score calculated: {Score} for ROE: {ROE}", score, roe);
        return score;
    }

    private decimal CalculateDividendYieldScore(decimal? dividendYield)
    {
        if (!dividendYield.HasValue)
        {
            _logger.LogDebug("Invalid Dividend Yield: {DividendYield}", dividendYield);
            return 0;
        }

        var score = dividendYield.Value switch
        {
            < 0 => 0,
            <= 3 => dividendYield.Value * 20,
            <= 8 => 60 + ((dividendYield.Value - 3) * 8),
            _ => Math.Max(0, 100 - ((dividendYield.Value - 8) * 10))
        };

        _logger.LogDebug("Dividend Yield Score calculated: {Score} for yield: {DividendYield}",
            score, dividendYield);
        return score;
    }

    private decimal CalculateDebtEquityScore(decimal? debtEquity)
    {
        if (!debtEquity.HasValue)
        {
            _logger.LogDebug("Invalid Debt/Equity ratio: {DebtEquity}", debtEquity);
            return 0;
        }

        var score = debtEquity.Value switch
        {
            < 0 => 0,
            <= 1 => 100 - (debtEquity.Value * 20),
            _ => Math.Max(0, 80 - ((debtEquity.Value - 1) * 30))
        };

        _logger.LogDebug("Debt/Equity Score calculated: {Score} for ratio: {DebtEquity}",
            score, debtEquity);
        return score;
    }

    private decimal CalculateNAVPriceScore(decimal nav, decimal price)
    {
        if (price <= 0)
        {
            _logger.LogDebug("Invalid price: {Price}", price);
            return 0;
        }

        // Calculate NAV/Price ratio (higher is better as it indicates undervaluation)
        // Make sure we're using the NAV value with two decimal places for consistency
        decimal roundedNav = Math.Round(nav, 2);
        decimal navPriceRatio = roundedNav / price;

        var score = navPriceRatio switch
        {
            < 0.5m => 30, // Significantly overvalued
            <= 0.8m => 50, // Moderately overvalued
            <= 1.0m => 70, // Fairly valued
            <= 1.2m => 85, // Moderately undervalued
            _ => 100 // Significantly undervalued
        };

        _logger.LogDebug("NAV/Price Score calculated: {Score} for ratio: {Ratio} (NAV: {NAV}, Price: {Price})",
            score, navPriceRatio, roundedNav, price);
        return score;
    }
}