namespace CSEInvestmentTool.Application.Models;

public class StockSymbolInfo
{
    public string Symbol { get; set; } = string.Empty;
    public long IssuedQuantity { get; set; }
    public decimal MarketPrice { get; set; }
}
