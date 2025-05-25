using CSEInvestmentTool.Application.Interfaces;
using CSEInvestmentTool.Application.Models.LLM;
using CSEInvestmentTool.Infrastructure.Services.LLMProviders;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CSEInvestmentTool.Infrastructure.Services;

public class LLMProviderFactory : ILLMProviderFactory
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILoggerFactory _loggerFactory;
    private readonly Dictionary<LLMProvider, LLMProviderConfig> _providerConfigs;

    public LLMProviderFactory(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILoggerFactory loggerFactory)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _loggerFactory = loggerFactory;
        _providerConfigs = LoadProviderConfigurations();
    }

    public ILLMProvider CreateProvider(LLMProvider provider)
    {
        if (!_providerConfigs.TryGetValue(provider, out var config))
        {
            throw new ArgumentException($"Provider {provider} is not configured", nameof(provider));
        }

        var httpClient = _httpClientFactory.CreateClient($"LLM_{provider}");

        return provider switch
        {
            LLMProvider.Deepseek => new DeepseekLLMProvider(
                httpClient,
                _loggerFactory.CreateLogger<DeepseekLLMProvider>(),
                config),

            // Future providers can be added here:
            // LLMProvider.OpenAI => new OpenAILLMProvider(httpClient, logger, config),
            // LLMProvider.Anthropic => new AnthropicLLMProvider(httpClient, logger, config),

            _ => throw new NotSupportedException($"Provider {provider} is not implemented yet")
        };
    }

    public ILLMProvider GetDefaultProvider()
    {
        // Get the default provider from configuration, fallback to Deepseek
        var defaultProviderName = _configuration["LLM:DefaultProvider"] ?? "Deepseek";

        if (Enum.TryParse<LLMProvider>(defaultProviderName, true, out var provider))
        {
            return CreateProvider(provider);
        }

        // Fallback to Deepseek if configuration is invalid
        return CreateProvider(LLMProvider.Deepseek);
    }

    public List<LLMProvider> GetAvailableProviders()
    {
        return _providerConfigs.Keys.ToList();
    }

    private Dictionary<LLMProvider, LLMProviderConfig> LoadProviderConfigurations()
    {
        var configs = new Dictionary<LLMProvider, LLMProviderConfig>();

        // Load Deepseek configuration
        var deepseekConfig = LoadProviderConfig("Deepseek", LLMProvider.Deepseek);
        if (deepseekConfig != null)
        {
            configs[LLMProvider.Deepseek] = deepseekConfig;
        }

        // Future: Load other provider configurations
        // var openaiConfig = LoadProviderConfig("OpenAI", LLMProvider.OpenAI);
        // if (openaiConfig != null) configs[LLMProvider.OpenAI] = openaiConfig;

        return configs;
    }

    private LLMProviderConfig? LoadProviderConfig(string sectionName, LLMProvider provider)
    {
        var section = _configuration.GetSection($"LLM:Providers:{sectionName}");

        if (!section.Exists())
        {
            return null;
        }

        var apiKey = section["ApiKey"];
        if (string.IsNullOrEmpty(apiKey))
        {
            return null;
        }

        return new LLMProviderConfig
        {
            ProviderName = sectionName,
            ApiKey = apiKey,
            ApiEndpoint = section["ApiEndpoint"] ?? GetDefaultEndpoint(provider),
            Model = section["Model"] ?? GetDefaultModel(provider),
            DefaultTemperature = decimal.Parse(section["Temperature"] ?? "0.7"),
            DefaultMaxTokens = int.Parse(section["MaxTokens"] ?? "2000")
        };
    }

    private static string GetDefaultEndpoint(LLMProvider provider)
    {
        return provider switch
        {
            LLMProvider.Deepseek => "https://api.deepseek.com/chat/completions",
            LLMProvider.OpenAI => "https://api.openai.com/v1/chat/completions",
            LLMProvider.Anthropic => "https://api.anthropic.com/v1/messages",
            _ => throw new NotSupportedException($"No default endpoint for provider {provider}")
        };
    }

    private static string GetDefaultModel(LLMProvider provider)
    {
        return provider switch
        {
            LLMProvider.Deepseek => "deepseek-chat", // Changed from "deepseek-r1" to correct model name
            LLMProvider.OpenAI => "gpt-4",
            LLMProvider.Anthropic => "claude-3-sonnet-20240229",
            _ => throw new NotSupportedException($"No default model for provider {provider}")
        };
    }
}