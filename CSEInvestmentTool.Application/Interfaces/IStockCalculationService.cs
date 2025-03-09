using CSEInvestmentTool.Application.Models;

namespace CSEInvestmentTool.Application.Interfaces;

public interface IStockCalculationService
{
    /// <summary>
    /// Calculates NAV based on total equity and issued quantity.
    /// </summary>
    Task<decimal> CalculateNAVAsync(string symbol, decimal totalEquity);

    /// <summary>
    /// Gets the total issued quantity for a company, considering all stock types.
    /// </summary>
    Task<long> GetTotalIssuedQuantityForCompanyAsync(string symbol);

    /// <summary>
    /// Gets the current market price for a symbol.
    /// </summary>
    Task<decimal> GetMarketPriceAsync(string symbol);

    /// <summary>
    /// Gets all related stock symbols for a company.
    /// </summary>
    Task<List<StockSymbolInfo>> GetRelatedStockSymbolsAsync(string symbol);

    /// <summary>
    /// Gets detailed stock information for a given symbol.
    /// </summary>
    Task<StockMarketData?> GetStockDataBySymbolAsync(string symbol);

    /// <summary>
    /// Gets a list of all companies for the company search dropdown
    /// </summary>
    Task<List<CompanySearchResult>> GetCompaniesForSearchAsync();

    /// <summary>
    /// Searches for companies by name (partial match)
    /// </summary>
    Task<List<CompanySearchResult>> SearchCompaniesByNameAsync(string searchTerm);

    /// <summary>
    /// Gets stock information for a specific company
    /// </summary>
    Task<List<StockSymbolInfo>> GetStockSymbolsForCompanyAsync(string companyName);
}