using CSEInvestmentTool.Application.Models.LLM;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace CSEInvestmentTool.Infrastructure.Services;

public interface ILLMCacheService
{
    Task<LLMResponse?> GetCachedResponseAsync(string cacheKey);
    Task SetCachedResponseAsync(string cacheKey, LLMResponse response, TimeSpan? expiration = null);
    string GenerateCacheKey(LLMRequest request, string providerName);
    void ClearCache();
}

public class LLMCacheService : ILLMCacheService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<LLMCacheService> _logger;
    private readonly TimeSpan _defaultExpiration = TimeSpan.FromHours(24); // Cache for 24 hours

    public LLMCacheService(IMemoryCache cache, ILogger<LLMCacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<LLMResponse?> GetCachedResponseAsync(string cacheKey)
    {
        try
        {
            if (_cache.TryGetValue(cacheKey, out string? cachedJson) && !string.IsNullOrEmpty(cachedJson))
            {
                var response = JsonSerializer.Deserialize<LLMResponse>(cachedJson);
                _logger.LogInformation("Cache hit for key: {CacheKey}", cacheKey[..Math.Min(20, cacheKey.Length)]);
                return response;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error retrieving cached response for key: {CacheKey}", cacheKey);
        }

        return null;
    }

    public async Task SetCachedResponseAsync(string cacheKey, LLMResponse response, TimeSpan? expiration = null)
    {
        try
        {
            var json = JsonSerializer.Serialize(response);
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration ?? _defaultExpiration,
                SlidingExpiration = TimeSpan.FromHours(6), // Refresh if accessed within 6 hours
                Priority = CacheItemPriority.Normal
            };

            _cache.Set(cacheKey, json, cacheOptions);
            _logger.LogInformation("Cached response for key: {CacheKey}", cacheKey[..Math.Min(20, cacheKey.Length)]);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error caching response for key: {CacheKey}", cacheKey);
        }
    }

    public string GenerateCacheKey(LLMRequest request, string providerName)
    {
        // Create a deterministic cache key based on request content
        var keyData = new
        {
            Provider = providerName,
            SystemPrompt = request.SystemPrompt,
            UserPrompt = request.UserPrompt,
            Temperature = request.Temperature,
            MaxTokens = request.MaxTokens
        };

        var json = JsonSerializer.Serialize(keyData);
        var hash = ComputeHash(json);

        return $"llm_cache_{providerName}_{hash}";
    }

    public void ClearCache()
    {
        if (_cache is MemoryCache memoryCache)
        {
            memoryCache.Clear();
            _logger.LogInformation("LLM cache cleared");
        }
    }

    private static string ComputeHash(string input)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(hashedBytes)[..16]; // Use first 16 characters
    }
}
