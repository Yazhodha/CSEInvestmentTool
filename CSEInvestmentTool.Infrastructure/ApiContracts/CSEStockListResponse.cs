using System.Text.Json.Serialization;

namespace CSEInvestmentTool.Infrastructure.ApiContracts;

public class CSEStockListResponse
{
    [JsonPropertyName("reqByMarketcap")]
    public List<CSEStockInfo> Stocks { get; set; } = new List<CSEStockInfo>();
}

public class CSEStockInfo
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("symbol")]
    public string Symbol { get; set; } = string.Empty;

    [JsonPropertyName("high")]
    public decimal? High { get; set; }

    [JsonPropertyName("low")]
    public decimal? Low { get; set; }

    [JsonPropertyName("percentageChange")]
    public decimal? PercentageChange { get; set; }

    [JsonPropertyName("change")]
    public decimal? Change { get; set; }

    [JsonPropertyName("price")]
    public decimal? Price { get; set; }

    [JsonPropertyName("quantity")]
    public int? Quantity { get; set; }

    [JsonPropertyName("issueDate")]
    public string? IssueDate { get; set; }

    [JsonPropertyName("sharevolume")]
    public long? ShareVolume { get; set; }

    [JsonPropertyName("tradevolume")]
    public int? TradeVolume { get; set; }

    [JsonPropertyName("turnover")]
    public decimal? Turnover { get; set; }

    [JsonPropertyName("lastTradedTime")]
    public long? LastTradedTime { get; set; }

    [JsonPropertyName("marketCap")]
    public decimal? MarketCap { get; set; }

    [JsonPropertyName("marketCapPercentage")]
    public decimal? MarketCapPercentage { get; set; }

    [JsonPropertyName("issuedQTY")]
    public long? IssuedQuantity { get; set; }
}