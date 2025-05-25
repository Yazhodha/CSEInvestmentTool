namespace CSEInvestmentTool.Application.Models.LLM;

public class LLMResponse
{
    public bool Success { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public int? TokensUsed { get; set; }
    public string? Model { get; set; }
    public Dictionary<string, object> AdditionalData { get; set; } = new();
}
