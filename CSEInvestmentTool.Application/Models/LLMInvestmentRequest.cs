using CSEInvestmentTool.Domain.Constants;

namespace CSEInvestmentTool.Application.Models;
public class LLMInvestmentRequest
{
    public InvestmentPhilosophyType Philosophy { get; set; }
    public decimal MonthlyBudget { get; set; }
    public List<StockDataForAnalysis> Stocks { get; set; } = new();
    public string? AdditionalInstructions { get; set; }
}
