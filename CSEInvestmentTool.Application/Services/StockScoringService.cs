using CSEInvestmentTool.Domain.Models;

namespace CSEInvestmentTool.Application.Services;

public class StockScoringService
{
    // Scoring weights for different parameters
    private static readonly decimal PE_WEIGHT = 0.25m;
    private static readonly decimal ROE_WEIGHT = 0.25m;
    private static readonly decimal DIVIDEND_WEIGHT = 0.20m;
    private static readonly decimal DEBT_EQUITY_WEIGHT = 0.15m;
    private static readonly decimal PROFIT_MARGIN_WEIGHT = 0.15m;

    public StockScore CalculateScore(FundamentalData data)
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
        score.ProfitMarginScore = CalculateProfitMarginScore(data.NetProfitMargin);

        // Calculate weighted total score
        score.TotalScore = (score.PEScore * PE_WEIGHT) +
                          (score.ROEScore * ROE_WEIGHT) +
                          (score.DividendYieldScore * DIVIDEND_WEIGHT) +
                          (score.DebtEquityScore * DEBT_EQUITY_WEIGHT) +
                          (score.ProfitMarginScore * PROFIT_MARGIN_WEIGHT);

        return score;
    }

    private decimal CalculatePEScore(decimal? peRatio)
    {
        if (!peRatio.HasValue || peRatio <= 0) return 0;
        
        // Lower P/E is better, but too low might be a warning sign
        // Optimal range: 5-15
        if (peRatio < 5) return 70;
        if (peRatio <= 15) return 100 - ((peRatio.Value - 5) * 3);
        if (peRatio <= 25) return 70 - ((peRatio.Value - 15) * 3);
        return Math.Max(0, 40 - ((peRatio.Value - 25) * 2));
    }

    private decimal CalculateROEScore(decimal? roe)
    {
        if (!roe.HasValue) return 0;
        
        // Higher ROE is better
        // < 0% : 0 points
        // 0-5% : 0-25 points
        // 5-15% : 25-75 points
        // 15-25% : 75-100 points
        // > 25% : 100 points
        if (roe < 0) return 0;
        if (roe <= 5) return roe.Value * 5;
        if (roe <= 15) return 25 + ((roe.Value - 5) * 5);
        if (roe <= 25) return 75 + ((roe.Value - 15) * 2.5m);
        return 100;
    }

    private decimal CalculateDividendYieldScore(decimal? dividendYield)
    {
        if (!dividendYield.HasValue) return 0;
        
        // Higher dividend yield is better, but too high might be unsustainable
        // Optimal range: 3-8%
        if (dividendYield < 0) return 0;
        if (dividendYield <= 3) return dividendYield.Value * 20;
        if (dividendYield <= 8) return 60 + ((dividendYield.Value - 3) * 8);
        return Math.Max(0, 100 - ((dividendYield.Value - 8) * 10));
    }

    private decimal CalculateDebtEquityScore(decimal? debtEquity)
    {
        if (!debtEquity.HasValue) return 0;
        
        // Lower debt/equity is better
        // Optimal: < 1.0
        if (debtEquity < 0) return 0;
        if (debtEquity <= 1) return 100 - (debtEquity.Value * 20);
        return Math.Max(0, 80 - ((debtEquity.Value - 1) * 30));
    }

    private decimal CalculateProfitMarginScore(decimal? profitMargin)
    {
        if (!profitMargin.HasValue) return 0;
        
        // Higher profit margin is better
        // < 0% : 0 points
        // 0-5% : 0-30 points
        // 5-15% : 30-70 points
        // 15-25% : 70-100 points
        // > 25% : 100 points
        if (profitMargin < 0) return 0;
        if (profitMargin <= 5) return profitMargin.Value * 6;
        if (profitMargin <= 15) return 30 + ((profitMargin.Value - 5) * 4);
        if (profitMargin <= 25) return 70 + ((profitMargin.Value - 15) * 3);
        return 100;
    }
}