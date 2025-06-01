using CSEInvestmentTool.Application.Models.LLM;

namespace CSEInvestmentTool.Application.Interfaces;

public interface ILLMCacheService
{
    Task<LLMResponse?> GetCachedResponseAsync(string cacheKey);
    Task SetCachedResponseAsync(string cacheKey, LLMResponse response, TimeSpan? expiration = null);
    string GenerateCacheKey(LLMRequest request, string providerName);
    void ClearCache();
}
