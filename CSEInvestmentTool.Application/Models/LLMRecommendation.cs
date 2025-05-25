namespace CSEInvestmentTool.Application.Models;

public class LLMRecommendation
{
    public int StockId { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public decimal RecommendedAmount { get; set; }
    public int RecommendationRank { get; set; }
    public string Reasoning { get; set; } = string.Empty;
    public string RiskAssessment { get; set; } = string.Empty;
    public decimal ConfidenceScore { get; set; } // 0-100
    public List<string> StrengthFactors { get; set; } = new();
    public List<string> ConcernFactors { get; set; } = new();
}
