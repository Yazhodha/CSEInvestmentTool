using System.Net.Http;
using HtmlAgilityPack;
using CSEInvestmentTool.Domain.Models;

namespace CSEInvestmentTool.Infrastructure.Services;

public interface IDataCollectionService
{
    Task<List<Stock>> GetAllStocksAsync();
    Task<FundamentalData> GetFundamentalDataAsync(string symbol);
}

public class CSEDataCollectionService : IDataCollectionService
{
    private readonly HttpClient _httpClient;
    private const string CSE_BASE_URL = "https://www.cse.lk";
    
    public CSEDataCollectionService(HttpClient httpClient)
    {
        _httpClient = httpClient;
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

            return stocks;
        }
        catch (Exception ex)
        {
            // Log error
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

            // Extract P/E Ratio
            var peNode = doc.DocumentNode.SelectSingleNode("//td[contains(text(),'P/E Ratio')]/following-sibling::td");
            if (peNode != null)
            {
                decimal.TryParse(peNode.InnerText.Trim(), out decimal peRatio);
                fundamentalData.PERatio = peRatio;
            }

            // Extract other fundamental data...
            // Note: Actual XPath selectors would need to be adjusted based on CSE website structure

            return fundamentalData;
        }
        catch (Exception ex)
        {
            // Log error
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
