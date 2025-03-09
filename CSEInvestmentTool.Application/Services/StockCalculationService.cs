using CSEInvestmentTool.Application.Interfaces;
using CSEInvestmentTool.Application.Models;
using Microsoft.Extensions.Logging;

namespace CSEInvestmentTool.Application.Services;

public class StockCalculationService : IStockCalculationService
{
    private readonly ICSEApiService _cseApiService;
    private readonly ILogger<StockCalculationService> _logger;

    public StockCalculationService(ICSEApiService cseApiService, ILogger<StockCalculationService> logger)
    {
        _cseApiService = cseApiService;
        _logger = logger;
    }

    public async Task<decimal> CalculateNAVAsync(string symbol, decimal totalEquity)
    {
        var totalIssuedQty = await GetTotalIssuedQuantityForCompanyAsync(symbol);

        if (totalIssuedQty <= 0)
        {
            _logger.LogWarning("Total issued quantity is zero or negative for symbol {Symbol}", symbol);
            return 0;
        }

        // Calculate NAV and round to 2 decimal places
        var nav = Math.Round(totalEquity / totalIssuedQty, 2, MidpointRounding.AwayFromZero);
        _logger.LogInformation("Calculated NAV for {Symbol}: {NAV} (Total Equity: {TotalEquity}, Total Issued Qty: {TotalIssuedQty})",
            symbol, nav, totalEquity, totalIssuedQty);

        return nav;
    }

    public async Task<long> GetTotalIssuedQuantityForCompanyAsync(string symbol)
    {
        if (string.IsNullOrEmpty(symbol))
        {
            _logger.LogWarning("Symbol cannot be null or empty");
            return 0;
        }

        try
        {
            // Get the stock data for the provided symbol
            var stockData = await _cseApiService.GetStockDataBySymbolAsync(symbol);
            if (stockData == null)
            {
                _logger.LogWarning("No stock data found for symbol {Symbol}", symbol);
                return 0;
            }

            // Get all stocks for the same company
            var companyStocks = await _cseApiService.GetAllStocksByCompanyNameAsync(stockData.CompanyName);

            // Sum up all issued quantities
            var totalIssuedQty = companyStocks.Sum(s => s.IssuedQuantity);

            _logger.LogInformation("Total issued quantity for company {CompanyName} ({Symbol}): {TotalIssuedQty}",
                stockData.CompanyName, symbol, totalIssuedQty);

            return totalIssuedQty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating total issued quantity for {Symbol}", symbol);
            return 0;
        }
    }

    public async Task<decimal> GetMarketPriceAsync(string symbol)
    {
        if (string.IsNullOrEmpty(symbol))
        {
            _logger.LogWarning("Symbol cannot be null or empty");
            return 0;
        }

        try
        {
            var stockData = await _cseApiService.GetStockDataBySymbolAsync(symbol);

            if (stockData == null)
            {
                _logger.LogWarning("No stock data found for symbol {Symbol}", symbol);
                return 0;
            }

            // Round to 2 decimal places for consistency
            return Math.Round(stockData.MarketPrice, 2, MidpointRounding.AwayFromZero);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting market price for {Symbol}", symbol);
            return 0;
        }
    }

    public async Task<List<StockSymbolInfo>> GetRelatedStockSymbolsAsync(string symbol)
    {
        if (string.IsNullOrEmpty(symbol))
        {
            _logger.LogWarning("Symbol cannot be null or empty");
            return new List<StockSymbolInfo>();
        }

        try
        {
            // Get the stock data for the provided symbol
            var stockData = await _cseApiService.GetStockDataBySymbolAsync(symbol);
            if (stockData == null)
            {
                _logger.LogWarning("No stock data found for symbol {Symbol}", symbol);
                return new List<StockSymbolInfo>();
            }

            // Get all stocks for the same company
            var companyStocks = await _cseApiService.GetAllStocksByCompanyNameAsync(stockData.CompanyName);

            return companyStocks.Select(s => new StockSymbolInfo
            {
                Symbol = s.Symbol,
                IssuedQuantity = s.IssuedQuantity,
                MarketPrice = Math.Round(s.MarketPrice, 2, MidpointRounding.AwayFromZero) // Round for consistency
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting related stock symbols for {Symbol}", symbol);
            return new List<StockSymbolInfo>();
        }
    }

    public async Task<StockMarketData?> GetStockDataBySymbolAsync(string symbol)
    {
        if (string.IsNullOrEmpty(symbol))
        {
            _logger.LogWarning("Symbol cannot be null or empty");
            return null;
        }

        try
        {
            var stockData = await _cseApiService.GetStockDataBySymbolAsync(symbol);

            if (stockData != null)
            {
                // Round market price to 2 decimal places for consistency
                stockData.MarketPrice = Math.Round(stockData.MarketPrice, 2, MidpointRounding.AwayFromZero);
            }

            return stockData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting stock data for {Symbol}", symbol);
            return null;
        }
    }

    public async Task<List<CompanySearchResult>> GetCompaniesForSearchAsync()
    {
        try
        {
            return await _cseApiService.GetCompanyListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting companies for search");
            return new List<CompanySearchResult>();
        }
    }

    public async Task<List<CompanySearchResult>> SearchCompaniesByNameAsync(string searchTerm)
    {
        try
        {
            return await _cseApiService.SearchCompaniesByNameAsync(searchTerm);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching companies by name: {SearchTerm}", searchTerm);
            return new List<CompanySearchResult>();
        }
    }

    public async Task<List<StockSymbolInfo>> GetStockSymbolsForCompanyAsync(string companyName)
    {
        if (string.IsNullOrEmpty(companyName))
        {
            _logger.LogWarning("Company name cannot be null or empty");
            return new List<StockSymbolInfo>();
        }

        try
        {
            // Get all stocks for the company
            var companyStocks = await _cseApiService.GetAllStocksByCompanyNameAsync(companyName);

            return companyStocks.Select(s => new StockSymbolInfo
            {
                Symbol = s.Symbol,
                IssuedQuantity = s.IssuedQuantity,
                MarketPrice = Math.Round(s.MarketPrice, 2, MidpointRounding.AwayFromZero) // Round for consistency
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting stock symbols for company: {CompanyName}", companyName);
            return new List<StockSymbolInfo>();
        }
    }
}