namespace CSEInvestmentTool.Application.Models;

public class LLMInvestmentResponse
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public List<LLMRecommendation> Recommendations { get; set; } = new();
    public string OverallAnalysis { get; set; } = string.Empty;
    public string MarketOutlook { get; set; } = string.Empty;
    public List<string> RiskFactors { get; set; } = new();
    public string LLMProvider { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
}
