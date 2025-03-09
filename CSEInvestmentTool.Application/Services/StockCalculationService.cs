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

        var nav = totalEquity / totalIssuedQty;
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

            return stockData.MarketPrice;
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
                MarketPrice = s.MarketPrice
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
            return await _cseApiService.GetStockDataBySymbolAsync(symbol);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting stock data for {Symbol}", symbol);
            return null;
        }
    }
}

