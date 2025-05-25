using CSEInvestmentTool.Application.Interfaces;
using CSEInvestmentTool.Application.Models.LLM;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CSEInvestmentTool.Infrastructure.Services.LLMProviders;

public class DeepseekLLMProvider : ILLMProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<DeepseekLLMProvider> _logger;
    private readonly LLMProviderConfig _config;

    public string ProviderName => "Deepseek";
    public string Model => _config.Model;

    public DeepseekLLMProvider(HttpClient httpClient, ILogger<DeepseekLLMProvider> logger, LLMProviderConfig config)
    {
        _httpClient = httpClient;
        _logger = logger;
        _config = config;

        // Configure HttpClient for Deepseek API
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _config.ApiKey);
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
    }

    public async Task<LLMResponse> CompleteChatAsync(LLMRequest request)
    {
        try
        {
            _logger.LogInformation("Sending request to Deepseek API with model {Model}", _config.Model);

            var requestBody = new DeepseekChatRequest
            {
                Model = _config.Model,
                Messages = new[]
                {
                    new DeepseekMessage { Role = "system", Content = request.SystemPrompt },
                    new DeepseekMessage { Role = "user", Content = request.UserPrompt }
                },
                Temperature = (double)request.Temperature,
                MaxTokens = request.MaxTokens,
                Stream = false
            };

            var json = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });

            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _logger.LogDebug("Deepseek API request: {RequestBody}", json);

            var response = await _httpClient.PostAsync(_config.ApiEndpoint, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            _logger.LogDebug("Deepseek API response status: {StatusCode}", response.StatusCode);
            _logger.LogDebug("Deepseek API response: {ResponseContent}", responseContent);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Deepseek API error: {StatusCode} - {Content}", response.StatusCode, responseContent);
                return new LLMResponse
                {
                    Success = false,
                    ErrorMessage = $"Deepseek API error: {response.StatusCode} - {responseContent}"
                };
            }

            var deepseekResponse = JsonSerializer.Deserialize<DeepseekChatResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                PropertyNameCaseInsensitive = true
            });

            if (deepseekResponse?.Choices?.Any() != true)
            {
                return new LLMResponse
                {
                    Success = false,
                    ErrorMessage = "No response content received from Deepseek API"
                };
            }

            var firstChoice = deepseekResponse.Choices.First();
            return new LLMResponse
            {
                Success = true,
                Content = firstChoice.Message?.Content ?? "",
                TokensUsed = deepseekResponse.Usage?.TotalTokens,
                Model = deepseekResponse.Model ?? _config.Model,
                AdditionalData = new Dictionary<string, object>
                {
                    ["prompt_tokens"] = deepseekResponse.Usage?.PromptTokens ?? 0,
                    ["completion_tokens"] = deepseekResponse.Usage?.CompletionTokens ?? 0,
                    ["finish_reason"] = firstChoice.FinishReason ?? ""
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Deepseek API");
            return new LLMResponse
            {
                Success = false,
                ErrorMessage = $"Error calling Deepseek API: {ex.Message}"
            };
        }
    }

    public async Task<bool> IsAvailableAsync()
    {
        try
        {
            // Test with a simple request
            var testRequest = new LLMRequest
            {
                SystemPrompt = "You are a helpful assistant.",
                UserPrompt = "Say 'Hello' if you can hear me.",
                Temperature = 0.1m,
                MaxTokens = 10
            };

            var response = await CompleteChatAsync(testRequest);
            return response.Success;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Deepseek provider availability check failed");
            return false;
        }
    }

    public decimal? EstimateCost(int inputTokens, int outputTokens)
    {
        // Deepseek R1 pricing (as of 2024 - very affordable)
        // Input: $0.14 per 1M tokens
        // Output: $0.28 per 1M tokens
        const decimal inputCostPer1M = 0.14m;
        const decimal outputCostPer1M = 0.28m;

        var inputCost = (inputTokens / 1_000_000m) * inputCostPer1M;
        var outputCost = (outputTokens / 1_000_000m) * outputCostPer1M;

        return inputCost + outputCost;
    }

    // Deepseek API Models (OpenAI-compatible format)
    private class DeepseekChatRequest
    {
        public string Model { get; set; } = "";
        public DeepseekMessage[] Messages { get; set; } = Array.Empty<DeepseekMessage>();
        public double Temperature { get; set; } = 0.7;
        public int MaxTokens { get; set; } = 2000;
        public bool Stream { get; set; } = false;
    }

    private class DeepseekMessage
    {
        public string Role { get; set; } = "";
        public string Content { get; set; } = "";
    }

    private class DeepseekChatResponse
    {
        public string? Id { get; set; }
        public string? Object { get; set; }
        public long? Created { get; set; }
        public string? Model { get; set; }
        public DeepseekChoice[]? Choices { get; set; }
        public DeepseekUsage? Usage { get; set; }
    }

    private class DeepseekChoice
    {
        public int? Index { get; set; }
        public DeepseekMessage? Message { get; set; }
        public string? FinishReason { get; set; }
    }

    private class DeepseekUsage
    {
        public int? PromptTokens { get; set; }
        public int? CompletionTokens { get; set; }
        public int? TotalTokens { get; set; }
    }
}