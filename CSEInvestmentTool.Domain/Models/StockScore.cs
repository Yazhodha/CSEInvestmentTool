using System;

namespace CSEInvestmentTool.Domain.Models;

public class StockScore
{
    public int ScoreId { get; set; }
    public int StockId { get; set; }
    public DateTime ScoreDate { get; set; }
    public decimal PEScore { get; set; }
    public decimal ROEScore { get; set; }
    public decimal DividendYieldScore { get; set; }
    public decimal DebtEquityScore { get; set; }
    public decimal ProfitMarginScore { get; set; }
    public decimal TotalScore { get; set; }
    public int Rank { get; set; }
    public DateTime LastUpdated { get; set; }
    
    public Stock Stock { get; set; } = new Stock();
}
