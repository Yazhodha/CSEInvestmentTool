using CSEInvestmentTool.Domain.Models;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;

namespace CSEInvestmentTool.Infrastructure.Services;

public interface IDataCollectionService
{
    Task<List<Stock>> GetAllStocksAsync();
    Task<FundamentalData> GetFundamentalDataAsync(string symbol);
}

public class CSEDataCollectionService : IDataCollectionService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CSEDataCollectionService> _logger;
    private const string CSE_BASE_URL = "https://www.cse.lk";

    public CSEDataCollectionService(HttpClient httpClient, ILogger<CSEDataCollectionService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _httpClient.BaseAddress = new Uri(CSE_BASE_URL);
    }

    public async Task<List<Stock>> GetAllStocksAsync()
    {
        try
        {
            var response = await _httpClient.GetStringAsync("/market");
            var doc = new HtmlDocument();
            doc.LoadHtml(response);

            var stocks = new List<Stock>();
            var stockNodes = doc.DocumentNode.SelectNodes("//table[@id='stocks-table']//tr");

            if (stockNodes != null)
            {
                foreach (var node in stockNodes.Skip(1)) // Skip header row
                {
                    var columns = node.SelectNodes("td");
                    if (columns?.Count >= 3)
                    {
                        stocks.Add(new Stock
                        {
                            Symbol = columns[0].InnerText.Trim(),
                            CompanyName = columns[1].InnerText.Trim(),
                            Sector = columns[2].InnerText.Trim(),
                            IsActive = true,
                            LastUpdated = DateTime.UtcNow
                        });
                    }
                }
            }

            _logger.LogInformation("Successfully retrieved {Count} stocks from CSE", stocks.Count);
            return stocks;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get stocks list from CSE");
            throw new DataCollectionException("Failed to get stocks list", ex);
        }
    }

    public async Task<FundamentalData> GetFundamentalDataAsync(string symbol)
    {
        try
        {
            var response = await _httpClient.GetStringAsync($"/company/{symbol}");
            var doc = new HtmlDocument();
            doc.LoadHtml(response);

            var fundamentalData = new FundamentalData
            {
                Date = DateTime.UtcNow.Date,
                LastUpdated = DateTime.UtcNow
            };

            // Market Price
            var marketPriceNode = doc.DocumentNode.SelectSingleNode("//td[contains(text(),'Market Price')]/following-sibling::td");
            if (marketPriceNode != null && decimal.TryParse(marketPriceNode.InnerText.Trim(), out decimal marketPrice))
            {
                fundamentalData.MarketPrice = marketPrice;
            }

            // NAV
            var navNode = doc.DocumentNode.SelectSingleNode("//td[contains(text(),'Net Asset Value')]/following-sibling::td");
            if (navNode != null && decimal.TryParse(navNode.InnerText.Trim(), out decimal nav))
            {
                fundamentalData.NAV = nav;
            }

            // EPS
            var epsNode = doc.DocumentNode.SelectSingleNode("//td[contains(text(),'Earnings Per Share')]/following-sibling::td");
            if (epsNode != null && decimal.TryParse(epsNode.InnerText.Trim(), out decimal eps))
            {
                fundamentalData.EPS = eps;
            }

            // Annual Dividend
            var dividendNode = doc.DocumentNode.SelectSingleNode("//td[contains(text(),'Total Dividend')]/following-sibling::td");
            if (dividendNode != null && decimal.TryParse(dividendNode.InnerText.Trim(), out decimal dividend))
            {
                fundamentalData.AnnualDividend = dividend;
            }

            // Total Liabilities and Equity would typically come from financial statements
            // For now, these might need to be entered manually or sourced from a different endpoint
            // This is placeholder logic that would need to be updated based on actual data availability
            var balanceSheetNodes = doc.DocumentNode.SelectNodes("//td[contains(text(),'Total Liabilities') or contains(text(),'Total Equity')]/following-sibling::td");
            if (balanceSheetNodes?.Count >= 2)
            {
                if (decimal.TryParse(balanceSheetNodes[0].InnerText.Trim(), out decimal liabilities))
                {
                    fundamentalData.TotalLiabilities = liabilities;
                }
                if (decimal.TryParse(balanceSheetNodes[1].InnerText.Trim(), out decimal equity))
                {
                    fundamentalData.TotalEquity = equity;
                }
            }

            _logger.LogInformation("Successfully retrieved fundamental data for stock {Symbol}", symbol);
            return fundamentalData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get fundamental data for {Symbol}", symbol);
            throw new DataCollectionException($"Failed to get fundamental data for {symbol}", ex);
        }
    }
}

public class DataCollectionException : Exception
{
    public DataCollectionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}