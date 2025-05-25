using CSEInvestmentTool.Application.Models.LLM;

namespace CSEInvestmentTool.Application.Interfaces;

/// <summary>
/// Generic interface for LLM providers (OpenAI, Deepseek, Anthropic, etc.)
/// </summary>
public interface ILLMProvider
{
    /// <summary>
    /// The name of the LLM provider
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// The model being used by this provider
    /// </summary>
    string Model { get; }

    /// <summary>
    /// Sends a generic text completion request to the LLM
    /// </summary>
    /// <param name="request">The LLM request</param>
    /// <returns>The LLM response</returns>
    Task<LLMResponse> CompleteChatAsync(LLMRequest request);

    /// <summary>
    /// Checks if the provider is properly configured and available
    /// </summary>
    /// <returns>True if the provider is available</returns>
    Task<bool> IsAvailableAsync();

    /// <summary>
    /// Gets the estimated cost for a request (if supported)
    /// </summary>
    /// <param name="inputTokens">Estimated input tokens</param>
    /// <param name="outputTokens">Estimated output tokens</param>
    /// <returns>Estimated cost in USD</returns>
    decimal? EstimateCost(int inputTokens, int outputTokens);
}

/// <summary>
/// Factory interface for creating LLM providers
/// </summary>
public interface ILLMProviderFactory
{
    /// <summary>
    /// Creates a specific LLM provider instance
    /// </summary>
    /// <param name="provider">The provider type</param>
    /// <returns>The LLM provider instance</returns>
    ILLMProvider CreateProvider(LLMProvider provider);

    /// <summary>
    /// Gets the default configured provider
    /// </summary>
    /// <returns>The default LLM provider</returns>
    ILLMProvider GetDefaultProvider();

    /// <summary>
    /// Gets all available providers
    /// </summary>
    /// <returns>List of available providers</returns>
    List<LLMProvider> GetAvailableProviders();
}