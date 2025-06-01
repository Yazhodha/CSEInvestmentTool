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
    private readonly ILLMCacheService _cacheService;

    public string ProviderName => "Deepseek";
    public string Model => _config.Model;

    public DeepseekLLMProvider(
        HttpClient httpClient,
        ILogger<DeepseekLLMProvider> logger,
        LLMProviderConfig config,
        ILLMCacheService cacheService)
    {
        _httpClient = httpClient;
        _logger = logger;
        _config = config;
        _cacheService = cacheService;

        // Configure HttpClient for Deepseek API with longer timeout
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _config.ApiKey);
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        _httpClient.Timeout = TimeSpan.FromMinutes(5); // Increase timeout for complex analysis
    }

    public async Task<LLMResponse> CompleteChatAsync(LLMRequest request)
    {
        var cacheKey = _cacheService.GenerateCacheKey(request, ProviderName);
        var maxRetries = 2; // Allow 2 retries for empty responses

        for (int attempt = 0; attempt <= maxRetries; attempt++)
        {
            try
            {
                // Check cache first (but only on first attempt)
                if (attempt == 0)
                {
                    var cachedResponse = await _cacheService.GetCachedResponseAsync(cacheKey);
                    if (cachedResponse != null && !string.IsNullOrWhiteSpace(cachedResponse.Content))
                    {
                        _logger.LogInformation("Returning valid cached response for Deepseek request");
                        return cachedResponse;
                    }
                    else if (cachedResponse != null)
                    {
                        _logger.LogWarning("Found cached response with empty content, clearing cache and retrying");
                        // Don't return the empty cached response, continue to make API call
                    }
                }

                _logger.LogInformation("Sending request to Deepseek API with model {Model} (Attempt {Attempt})", _config.Model, attempt + 1);

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
                _logger.LogDebug("Deepseek API raw response: {ResponseContent}", responseContent);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Deepseek API error: {StatusCode} - {Content}", response.StatusCode, responseContent);

                    var errorMessage = $"Deepseek API error: {response.StatusCode}";
                    try
                    {
                        var errorResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
                        if (errorResponse.TryGetProperty("error", out var error))
                        {
                            if (error.TryGetProperty("message", out var message))
                            {
                                errorMessage = $"Deepseek API error: {message.GetString()}";
                            }
                            if (error.TryGetProperty("type", out var errorType))
                            {
                                errorMessage += $" (Type: {errorType.GetString()})";
                            }

                            if (errorType.GetString()?.Contains("invalid_request_error") == true &&
                                message.GetString()?.Contains("Model Not Exist") == true)
                            {
                                errorMessage += $" - The model '{_config.Model}' is not available. Try 'deepseek-chat' or 'deepseek-coder' instead.";
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to parse error response details");
                    }

                    return new LLMResponse
                    {
                        Success = false,
                        ErrorMessage = errorMessage
                    };
                }

                var deepseekResponse = JsonSerializer.Deserialize<DeepseekChatResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                    PropertyNameCaseInsensitive = true
                });

                // Add detailed debugging
                _logger.LogDebug("=== DEEPSEEK RESPONSE STRUCTURE DEBUG ===");
                _logger.LogDebug("Response deserialized successfully: {Success}", deepseekResponse != null);
                _logger.LogDebug("Choices array length: {Length}", deepseekResponse?.Choices?.Length ?? 0);

                if (deepseekResponse?.Choices?.Any() == true)
                {
                    for (int i = 0; i < deepseekResponse.Choices.Length; i++)
                    {
                        var choice = deepseekResponse.Choices[i];
                        _logger.LogDebug("Choice {Index}: Index={ChoiceIndex}, FinishReason={FinishReason}",
                            i, choice.Index, choice.FinishReason);
                        _logger.LogDebug("Choice {Index}: Message is null: {IsNull}", i, choice.Message == null);

                        if (choice.Message != null)
                        {
                            _logger.LogDebug("Choice {Index}: Message.Role={Role}", i, choice.Message.Role);
                            _logger.LogDebug("Choice {Index}: Message.Content length={Length}", i, choice.Message.Content?.Length ?? 0);
                            _logger.LogDebug("Choice {Index}: Message.ReasoningContent length={Length}", i, choice.Message.ReasoningContent?.Length ?? 0);
                        }
                    }
                }
                _logger.LogDebug("=== END RESPONSE STRUCTURE DEBUG ===");

                if (deepseekResponse?.Choices?.Any() != true)
                {
                    _logger.LogWarning("No choices in Deepseek response. Full response: {ResponseContent}", responseContent);
                    if (attempt < maxRetries)
                    {
                        _logger.LogInformation("Retrying request due to no choices...");
                        await Task.Delay(1000);
                        continue;
                    }
                    return new LLMResponse
                    {
                        Success = false,
                        ErrorMessage = "No response content received from Deepseek API"
                    };
                }

                var firstChoice = deepseekResponse.Choices.First();

                // For deepseek-reasoner model, content might be in reasoning_content field
                string messageContent = "";

                if (!string.IsNullOrWhiteSpace(firstChoice.Message?.Content))
                {
                    messageContent = firstChoice.Message.Content;
                    _logger.LogDebug("Using content from 'content' field");
                }
                else if (!string.IsNullOrWhiteSpace(firstChoice.Message?.ReasoningContent))
                {
                    messageContent = firstChoice.Message.ReasoningContent;
                    _logger.LogDebug("Using content from 'reasoning_content' field");
                }

                // Check for truncated response
                if (firstChoice.FinishReason == "length")
                {
                    _logger.LogWarning("Response was truncated due to length limits. Consider increasing MaxTokens.");
                    if (string.IsNullOrWhiteSpace(messageContent))
                    {
                        if (attempt < maxRetries)
                        {
                            _logger.LogInformation("Retrying with higher token limit...");
                            // Increase MaxTokens for retry
                            request.MaxTokens = Math.Min(request.MaxTokens + 500, 4000);
                            await Task.Delay(1000);
                            continue;
                        }
                        return new LLMResponse
                        {
                            Success = false,
                            ErrorMessage = "Response was truncated and no content was generated. Try increasing MaxTokens or simplifying the prompt."
                        };
                    }
                }

                // Check if response is empty
                if (string.IsNullOrWhiteSpace(messageContent))
                {
                    _logger.LogWarning("Received empty response from Deepseek API on attempt {Attempt}", attempt + 1);

                    if (attempt < maxRetries)
                    {
                        _logger.LogInformation("Retrying request due to empty response...");
                        await Task.Delay(1000); // Wait 1 second before retry
                        continue; // Retry the request
                    }
                    else
                    {
                        return new LLMResponse
                        {
                            Success = false,
                            ErrorMessage = "Deepseek API returned empty response after multiple attempts"
                        };
                    }
                }

                _logger.LogInformation("Deepseek API response content: '{MessageContent}'", messageContent);

                var successResult = new LLMResponse
                {
                    Success = true,
                    Content = messageContent,
                    TokensUsed = deepseekResponse.Usage?.TotalTokens,
                    Model = deepseekResponse.Model ?? _config.Model,
                    AdditionalData = new Dictionary<string, object>
                    {
                        ["prompt_tokens"] = deepseekResponse.Usage?.PromptTokens ?? 0,
                        ["completion_tokens"] = deepseekResponse.Usage?.CompletionTokens ?? 0,
                        ["finish_reason"] = firstChoice.FinishReason ?? ""
                    }
                };

                // Only cache if we have valid content
                if (!string.IsNullOrWhiteSpace(messageContent))
                {
                    await _cacheService.SetCachedResponseAsync(cacheKey, successResult, TimeSpan.FromHours(24));
                }

                return successResult;
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                _logger.LogError(ex, "Deepseek API request timed out on attempt {Attempt}", attempt + 1);
                if (attempt < maxRetries)
                {
                    await Task.Delay(2000); // Wait 2 seconds before retry
                    continue;
                }
                return new LLMResponse
                {
                    Success = false,
                    ErrorMessage = "Request timed out after multiple attempts"
                };
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error calling Deepseek API on attempt {Attempt}", attempt + 1);
                if (attempt < maxRetries)
                {
                    await Task.Delay(1000);
                    continue;
                }
                return new LLMResponse
                {
                    Success = false,
                    ErrorMessage = $"Network error calling Deepseek API: {ex.Message}"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Deepseek API on attempt {Attempt}", attempt + 1);
                if (attempt < maxRetries)
                {
                    await Task.Delay(1000);
                    continue;
                }
                return new LLMResponse
                {
                    Success = false,
                    ErrorMessage = $"Error calling Deepseek API: {ex.Message}"
                };
            }
        }

        return new LLMResponse
        {
            Success = false,
            ErrorMessage = "Failed after multiple retry attempts"
        };
    }

    public async Task<bool> IsAvailableAsync()
    {
        try
        {
            var testRequest = new LLMRequest
            {
                SystemPrompt = "You are a helpful assistant.",
                UserPrompt = "Say 'Hello' if you can hear me.",
                Temperature = 0.1m,
                MaxTokens = 10
            };

            var response = await CompleteChatAsync(testRequest);

            if (!response.Success)
            {
                _logger.LogWarning("Deepseek provider availability check failed: {Error}", response.ErrorMessage);
            }

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

        [JsonPropertyName("reasoning_content")]
        public string? ReasoningContent { get; set; }
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