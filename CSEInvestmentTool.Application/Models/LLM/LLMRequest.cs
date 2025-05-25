namespace CSEInvestmentTool.Application.Models.LLM;

public class LLMRequest
{
    public string SystemPrompt { get; set; } = string.Empty;
    public string UserPrompt { get; set; } = string.Empty;
    public decimal Temperature { get; set; } = 0.7m;
    public int MaxTokens { get; set; } = 2000;
    public Dictionary<string, object> AdditionalParameters { get; set; } = new();
}
