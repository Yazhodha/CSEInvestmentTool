using System;

namespace CSEInvestmentTool.Domain.Models;

public class InvestmentRecommendation
{
    public int RecommendationId { get; set; }
    public int StockId { get; set; }
    public DateTime RecommendationDate { get; set; }
    public decimal RecommendedAmount { get; set; }
    public string RecommendationReason { get; set; } = string.Empty;
    public DateTime LastUpdated { get; set; }
    
    public Stock Stock { get; set; } = new Stock();
}
