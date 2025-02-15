using System;

namespace CSEInvestmentTool.Domain.Models;

public class FundamentalData
{
    public int FundamentalId { get; set; }
    public int StockId { get; set; }
    public DateTime Date { get; set; }
    public decimal? PERatio { get; set; }
    public decimal? ROE { get; set; }
    public decimal? DividendYield { get; set; }
    public decimal? DebtToEquityRatio { get; set; }
    public decimal? NetProfitMargin { get; set; }
    public decimal? MarketCap { get; set; }
    public long? TradingVolume { get; set; }
    public decimal? HighPrice { get; set; }
    public decimal? LowPrice { get; set; }
    public DateTime LastUpdated { get; set; }
    
    public Stock Stock { get; set; } = new Stock();
}
