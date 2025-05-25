namespace CSEInvestmentTool.Application.Models.LLM;

public class LLMProviderConfig
{
    public string ProviderName { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ApiEndpoint { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public decimal DefaultTemperature { get; set; } = 0.7m;
    public int DefaultMaxTokens { get; set; } = 2000;
    public Dictionary<string, string> AdditionalHeaders { get; set; } = new();
    public Dictionary<string, object> AdditionalConfig { get; set; } = new();
}
