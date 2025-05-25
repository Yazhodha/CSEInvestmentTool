namespace CSEInvestmentTool.Application.Models;

public class StockDataForAnalysis
{
    public int StockId { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string? Sector { get; set; }
    public decimal MarketPrice { get; set; }
    public decimal NAV { get; set; }
    public decimal EPS { get; set; }
    public decimal AnnualDividend { get; set; }
    public decimal TotalLiabilities { get; set; }
    public decimal TotalEquity { get; set; }

    // Calculated ratios
    public decimal? PERatio { get; set; }
    public decimal? ROE { get; set; }
    public decimal? DividendYield { get; set; }
    public decimal? DebtToEquityRatio { get; set; }
    public decimal? PBV { get; set; }
    public decimal? Return { get; set; }

    // Current algorithm score for comparison
    public decimal? CurrentAlgorithmScore { get; set; }
}
