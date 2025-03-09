using CSEInvestmentTool.Application.Interfaces;
using CSEInvestmentTool.Application.Models;
using CSEInvestmentTool.Infrastructure.ApiContracts;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text;

namespace CSEInvestmentTool.Infrastructure.Services;

public class CSEApiService : ICSEApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CSEApiService> _logger;
    private readonly IMemoryCache _cache;
    private const string CACHE_KEY = "CSE_STOCKS_DATA";
    private const string COMPANIES_CACHE_KEY = "CSE_COMPANIES_LIST";
    private const int CACHE_DURATION_MINUTES = 30;
    private const string CSE_API_URL = "https://www.cse.lk/api/list_by_market_cap";

    public CSEApiService(HttpClient httpClient, ILogger<CSEApiService> logger, IMemoryCache cache)
    {
        _httpClient = httpClient;
        _logger = logger;
        _cache = cache;
    }

    public async Task<List<StockMarketData>> GetAllStocksDataAsync()
    {
        // Try to get from cache first
        if (_cache.TryGetValue(CACHE_KEY, out List<StockMarketData>? cachedStocks) && cachedStocks != null)
        {
            _logger.LogInformation("Retrieved {Count} stocks from cache", cachedStocks.Count);
            return cachedStocks;
        }

        try
        {
            // Prepare the request content as required by the API
            var content = new StringContent("{\"headers\":{\"normalizedNames\":{},\"lazyUpdate\":null}}",
                Encoding.UTF8, "application/json");

            // Set required headers
            _httpClient.DefaultRequestHeaders.Add("accept", "application/json, text/plain, */*");
            _httpClient.DefaultRequestHeaders.Add("accept-language", "en");
            _httpClient.DefaultRequestHeaders.Add("origin", "https://www.cse.lk");
            _httpClient.DefaultRequestHeaders.Add("referer", "https://www.cse.lk/");

            // Make the API request
            var response = await _httpClient.PostAsync(CSE_API_URL, content);
            response.EnsureSuccessStatusCode();

            var cseResponse = await response.Content.ReadFromJsonAsync<CSEStockListResponse>();

            if (cseResponse == null || cseResponse.Stocks == null)
            {
                _logger.LogWarning("No stocks data received from CSE API");
                return new List<StockMarketData>();
            }

            // Map to our application model
            var stocksData = cseResponse.Stocks
                .Where(s => !string.IsNullOrEmpty(s.Symbol)) // Filter out any items without symbols
                .Select(s => new StockMarketData
                {
                    Id = s.Id,
                    CompanyName = s.Name,
                    Symbol = s.Symbol,
                    MarketPrice = s.Price ?? 0,
                    IssuedQuantity = s.IssuedQuantity ?? 0,
                    High = s.High,
                    Low = s.Low,
                    Change = s.Change,
                    PercentageChange = s.PercentageChange,
                    LastUpdated = DateTime.UtcNow
                })
                .ToList();

            _logger.LogInformation("Successfully retrieved {Count} stocks from CSE API", stocksData.Count);

            // Cache the result
            _cache.Set(CACHE_KEY, stocksData, TimeSpan.FromMinutes(CACHE_DURATION_MINUTES));

            return stocksData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get stocks list from CSE API");
            return new List<StockMarketData>();
        }
    }

    public async Task<StockMarketData?> GetStockDataBySymbolAsync(string symbol)
    {
        if (string.IsNullOrEmpty(symbol))
        {
            _logger.LogWarning("Symbol cannot be null or empty");
            return null;
        }

        var allStocks = await GetAllStocksDataAsync();
        return allStocks.FirstOrDefault(s => s.Symbol.Equals(symbol, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<List<StockMarketData>> GetAllStocksByCompanyNameAsync(string companyName)
    {
        if (string.IsNullOrEmpty(companyName))
        {
            _logger.LogWarning("Company name cannot be null or empty");
            return new List<StockMarketData>();
        }

        var allStocks = await GetAllStocksDataAsync();
        return allStocks
            .Where(s => s.CompanyName.Equals(companyName, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    public async Task<List<CompanySearchResult>> GetCompanyListAsync()
    {
        // Try to get from cache first
        if (_cache.TryGetValue(COMPANIES_CACHE_KEY, out List<CompanySearchResult>? cachedCompanies) && cachedCompanies != null)
        {
            _logger.LogInformation("Retrieved {Count} companies from cache", cachedCompanies.Count);
            return cachedCompanies;
        }

        try
        {
            var allStocks = await GetAllStocksDataAsync();

            // Group by company name to identify companies with multiple stock types
            var companies = allStocks
                .GroupBy(s => s.CompanyName)
                .Select(g => new CompanySearchResult
                {
                    CompanyName = g.Key,
                    Symbols = g.Select(s => s.Symbol).ToList()
                })
                .OrderBy(c => c.CompanyName)
                .ToList();

            _logger.LogInformation("Created company list with {Count} companies", companies.Count);

            // Cache the result
            _cache.Set(COMPANIES_CACHE_KEY, companies, TimeSpan.FromMinutes(CACHE_DURATION_MINUTES));

            return companies;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create company list");
            return new List<CompanySearchResult>();
        }
    }

    public async Task<List<CompanySearchResult>> SearchCompaniesByNameAsync(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            _logger.LogInformation("Empty search term, returning top companies");
            var allCompanies = await GetCompanyListAsync();
            return allCompanies.Take(10).ToList(); // Return top 10 companies for empty search
        }

        try
        {
            var allCompanies = await GetCompanyListAsync();

            // Search for companies containing the search term (case insensitive)
            var matchingCompanies = allCompanies
                .Where(c => c.CompanyName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                .OrderBy(c => c.CompanyName)
                .ToList();

            _logger.LogInformation("Found {Count} companies matching search term: {SearchTerm}",
                matchingCompanies.Count, searchTerm);

            return matchingCompanies;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search companies by name: {SearchTerm}", searchTerm);
            return new List<CompanySearchResult>();
        }
    }
}