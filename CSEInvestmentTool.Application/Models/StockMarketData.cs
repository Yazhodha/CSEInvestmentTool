namespace CSEInvestmentTool.Application.Models;

public class StockMarketData
{
    public int Id { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public decimal MarketPrice { get; set; }
    public long IssuedQuantity { get; set; }
    public DateTime? LastUpdated { get; set; }

    // Additional properties that might be useful
    public decimal? High { get; set; }
    public decimal? Low { get; set; }
    public decimal? Change { get; set; }
    public decimal? PercentageChange { get; set; }
}