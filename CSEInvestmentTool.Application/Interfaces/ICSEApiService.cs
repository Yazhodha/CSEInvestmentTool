using CSEInvestmentTool.Application.Models;

namespace CSEInvestmentTool.Application.Interfaces;

public interface ICSEApiService
{
    /// <summary>
    /// Fetches all stock data from the CSE API.
    /// </summary>
    /// <returns>List of stock market data</returns>
    Task<List<StockMarketData>> GetAllStocksDataAsync();

    /// <summary>
    /// Fetches stock data for a specific symbol from the CSE API.
    /// </summary>
    /// <param name="symbol">The stock symbol to look for</param>
    /// <returns>Stock market data for the specified symbol</returns>
    Task<StockMarketData?> GetStockDataBySymbolAsync(string symbol);

    /// <summary>
    /// Gets all stock symbols for a company by company name.
    /// This handles cases where a company has multiple stock types (e.g., voting and non-voting).
    /// </summary>
    /// <param name="companyName">The company name to search for</param>
    /// <returns>List of stock market data for the company</returns>
    Task<List<StockMarketData>> GetAllStocksByCompanyNameAsync(string companyName);

    /// <summary>
    /// Gets a list of all companies with their available stock symbols
    /// </summary>
    /// <returns>List of companies with their stock symbols</returns>
    Task<List<CompanySearchResult>> GetCompanyListAsync();

    /// <summary>
    /// Searches for companies by name (partial match)
    /// </summary>
    /// <param name="searchTerm">The company name search term</param>
    /// <returns>List of matching companies with their stock symbols</returns>
    Task<List<CompanySearchResult>> SearchCompaniesByNameAsync(string searchTerm);
}