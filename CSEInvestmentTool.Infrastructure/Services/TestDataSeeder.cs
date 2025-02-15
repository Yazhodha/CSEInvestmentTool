using CSEInvestmentTool.Application.Interfaces;
using CSEInvestmentTool.Domain.Models;

namespace CSEInvestmentTool.Infrastructure.Services;

public class TestDataSeeder
{
    private readonly IStockRepository _stockRepository;
    private readonly IFundamentalDataRepository _fundamentalRepository;
    private readonly IStockScoreRepository _scoreRepository;
    private readonly IInvestmentRecommendationRepository _recommendationRepository;

    public TestDataSeeder(
        IStockRepository stockRepository,
        IFundamentalDataRepository fundamentalRepository,
        IStockScoreRepository scoreRepository,
        IInvestmentRecommendationRepository recommendationRepository)
    {
        _stockRepository = stockRepository;
        _fundamentalRepository = fundamentalRepository;
        _scoreRepository = scoreRepository;
        _recommendationRepository = recommendationRepository;
    }

    public async Task SeedTestDataAsync()
    {
        // Sample CSE stocks with realistic data
        var stocks = new List<(Stock Stock, FundamentalData Fundamentals)>
        {
            (
                new Stock 
                { 
                    Symbol = "JKH", 
                    CompanyName = "John Keells Holdings PLC", 
                    Sector = "Diversified Holdings" 
                },
                new FundamentalData
                {
                    Date = DateTime.UtcNow.Date,
                    PERatio = 12.5m,
                    ROE = 15.8m,
                    DividendYield = 3.2m,
                    DebtToEquityRatio = 0.45m,
                    NetProfitMargin = 18.5m,
                    MarketCap = 185000000000m, // 185B LKR
                    TradingVolume = 250000,
                    HighPrice = 156.75m,
                    LowPrice = 142.50m
                }
            ),
            (
                new Stock 
                { 
                    Symbol = "COMB", 
                    CompanyName = "Commercial Bank of Ceylon PLC", 
                    Sector = "Banks" 
                },
                new FundamentalData
                {
                    Date = DateTime.UtcNow.Date,
                    PERatio = 8.2m,
                    ROE = 12.5m,
                    DividendYield = 4.8m,
                    DebtToEquityRatio = 0.85m,
                    NetProfitMargin = 22.3m,
                    MarketCap = 145000000000m, // 145B LKR
                    TradingVolume = 180000,
                    HighPrice = 89.90m,
                    LowPrice = 82.50m
                }
            ),
            (
                new Stock 
                { 
                    Symbol = "DIAL", 
                    CompanyName = "Dialog Axiata PLC", 
                    Sector = "Telecommunications" 
                },
                new FundamentalData
                {
                    Date = DateTime.UtcNow.Date,
                    PERatio = 10.1m,
                    ROE = 14.2m,
                    DividendYield = 2.8m,
                    DebtToEquityRatio = 0.62m,
                    NetProfitMargin = 15.8m,
                    MarketCap = 120000000000m, // 120B LKR
                    TradingVolume = 320000,
                    HighPrice = 14.80m,
                    LowPrice = 13.20m
                }
            ),
            (
                new Stock 
                { 
                    Symbol = "HNB", 
                    CompanyName = "Hatton National Bank PLC", 
                    Sector = "Banks" 
                },
                new FundamentalData
                {
                    Date = DateTime.UtcNow.Date,
                    PERatio = 7.8m,
                    ROE = 13.5m,
                    DividendYield = 5.2m,
                    DebtToEquityRatio = 0.78m,
                    NetProfitMargin = 24.5m,
                    MarketCap = 95000000000m, // 95B LKR
                    TradingVolume = 150000,
                    HighPrice = 142.50m,
                    LowPrice = 135.75m
                }
            ),
            (
                new Stock 
                { 
                    Symbol = "SAMP", 
                    CompanyName = "Sampath Bank PLC", 
                    Sector = "Banks" 
                },
                new FundamentalData
                {
                    Date = DateTime.UtcNow.Date,
                    PERatio = 8.5m,
                    ROE = 11.8m,
                    DividendYield = 4.5m,
                    DebtToEquityRatio = 0.82m,
                    NetProfitMargin = 21.8m,
                    MarketCap = 85000000000m, // 85B LKR
                    TradingVolume = 130000,
                    HighPrice = 45.90m,
                    LowPrice = 42.50m
                }
            ),
            (
                new Stock 
                { 
                    Symbol = "CTC", 
                    CompanyName = "Ceylon Tobacco Company PLC", 
                    Sector = "Beverage Food & Tobacco" 
                },
                new FundamentalData
                {
                    Date = DateTime.UtcNow.Date,
                    PERatio = 15.2m,
                    ROE = 85.5m, // High ROE due to nature of business
                    DividendYield = 6.8m,
                    DebtToEquityRatio = 0.15m,
                    NetProfitMargin = 35.2m,
                    MarketCap = 155000000000m, // 155B LKR
                    TradingVolume = 25000,
                    HighPrice = 825.00m,
                    LowPrice = 805.50m
                }
            ),
            (
                new Stock 
                { 
                    Symbol = "LOLC", 
                    CompanyName = "LOLC Holdings PLC", 
                    Sector = "Diversified Holdings" 
                },
                new FundamentalData
                {
                    Date = DateTime.UtcNow.Date,
                    PERatio = 11.2m,
                    ROE = 16.8m,
                    DividendYield = 2.5m,
                    DebtToEquityRatio = 0.72m,
                    NetProfitMargin = 19.5m,
                    MarketCap = 165000000000m, // 165B LKR
                    TradingVolume = 180000,
                    HighPrice = 87.50m,
                    LowPrice = 82.30m
                }
            )
        };

        // Add stocks and their fundamental data
        foreach (var (stock, fundamentals) in stocks)
        {
            await _stockRepository.AddStockAsync(stock);
            
            fundamentals.StockId = stock.StockId;
            await _fundamentalRepository.AddFundamentalDataAsync(fundamentals);

            // Calculate and add scores based on fundamentals
            var score = CalculateScore(fundamentals);
            score.StockId = stock.StockId;
            score.ScoreDate = DateTime.UtcNow.Date;
            await _scoreRepository.AddStockScoreAsync(score);
        }
    }

    private StockScore CalculateScore(FundamentalData fundamentals)
    {
        var score = new StockScore
        {
            ScoreDate = DateTime.UtcNow.Date,
            // PE Score (lower is better, but not too low)
            PEScore = fundamentals.PERatio switch
            {
                <= 0 => 0,
                <= 5 => 70,
                <= 15 => 100 - ((fundamentals.PERatio.Value - 5) * 3),
                <= 25 => 70 - ((fundamentals.PERatio.Value - 15) * 3),
                _ => Math.Max(0, 40 - ((fundamentals.PERatio.Value - 25) * 2))
            },

            // ROE Score (higher is better)
            ROEScore = fundamentals.ROE switch
            {
                <= 0 => 0,
                <= 5 => fundamentals.ROE.Value * 5,
                <= 15 => 25 + ((fundamentals.ROE.Value - 5) * 5),
                <= 25 => 75 + ((fundamentals.ROE.Value - 15) * 2.5m),
                _ => 100
            },

            // Dividend Yield Score
            DividendYieldScore = fundamentals.DividendYield switch
            {
                <= 0 => 0,
                <= 3 => fundamentals.DividendYield.Value * 20,
                <= 8 => 60 + ((fundamentals.DividendYield.Value - 3) * 8),
                _ => Math.Max(0, 100 - ((fundamentals.DividendYield.Value - 8) * 10))
            },

            // Debt/Equity Score (lower is better)
            DebtEquityScore = fundamentals.DebtToEquityRatio switch
            {
                <= 0 => 0,
                <= 1 => 100 - (fundamentals.DebtToEquityRatio.Value * 20),
                _ => Math.Max(0, 80 - ((fundamentals.DebtToEquityRatio.Value - 1) * 30))
            },

            // Profit Margin Score
            ProfitMarginScore = fundamentals.NetProfitMargin switch
            {
                <= 0 => 0,
                <= 5 => fundamentals.NetProfitMargin.Value * 6,
                <= 15 => 30 + ((fundamentals.NetProfitMargin.Value - 5) * 4),
                <= 25 => 70 + ((fundamentals.NetProfitMargin.Value - 15) * 3),
                _ => 100
            }
        };

        // Calculate total score (weighted average)
        score.TotalScore = (
            score.PEScore * 0.25m +
            score.ROEScore * 0.25m +
            score.DividendYieldScore * 0.20m +
            score.DebtEquityScore * 0.15m +
            score.ProfitMarginScore * 0.15m
        );

        return score;
    }
}