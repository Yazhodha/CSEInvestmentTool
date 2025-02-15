using Microsoft.AspNetCore.Mvc;
using CSEInvestmentTool.Application.Services;
using CSEInvestmentTool.Domain.Models;
using CSEInvestmentTool.Infrastructure.Services;

namespace CSEInvestmentTool.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InvestmentController : ControllerBase
{
    private readonly IDataCollectionService _dataService;
    private readonly StockScoringService _scoringService;
    private readonly InvestmentAllocationService _allocationService;

    public InvestmentController(
        IDataCollectionService dataService,
        StockScoringService scoringService,
        InvestmentAllocationService allocationService)
    {
        _dataService = dataService;
        _scoringService = scoringService;
        _allocationService = allocationService;
    }

    [HttpGet("stocks")]
    public async Task<ActionResult<IEnumerable<Stock>>> GetStocks()
    {
        try
        {
            var stocks = await _dataService.GetAllStocksAsync();
            return Ok(stocks);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error retrieving stocks: {ex.Message}");
        }
    }

    [HttpGet("fundamentals/{symbol}")]
    public async Task<ActionResult<FundamentalData>> GetFundamentals(string symbol)
    {
        try
        {
            var fundamentals = await _dataService.GetFundamentalDataAsync(symbol);
            return Ok(fundamentals);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error retrieving fundamental data: {ex.Message}");
        }
    }

    [HttpGet("scores")]
    public async Task<ActionResult<IEnumerable<StockScore>>> GetScores()
    {
        try
        {
            var stocks = await _dataService.GetAllStocksAsync();
            var scores = new List<StockScore>();

            foreach (var stock in stocks)
            {
                var fundamentals = await _dataService.GetFundamentalDataAsync(stock.Symbol);
                var score = _scoringService.CalculateScore(fundamentals);
                scores.Add(score);
            }

            return Ok(scores);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error calculating scores: {ex.Message}");
        }
    }

    [HttpGet("recommendations")]
    public async Task<ActionResult<IEnumerable<InvestmentRecommendation>>> GetRecommendations()
    {
        try
        {
            // Get scores first
            var stocks = await _dataService.GetAllStocksAsync();
            var scores = new List<StockScore>();

            foreach (var stock in stocks)
            {
                var fundamentals = await _dataService.GetFundamentalDataAsync(stock.Symbol);
                var score = _scoringService.CalculateScore(fundamentals);
                scores.Add(score);
            }

            // Calculate recommendations
            var recommendations = _allocationService.CalculateInvestmentAllocations(
                scores,
                DateTime.UtcNow.Date);

            return Ok(recommendations);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error generating recommendations: {ex.Message}");
        }
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult<DashboardData>> GetDashboardData()
    {
        try
        {
            var stocks = await _dataService.GetAllStocksAsync();
            var scores = new List<StockScore>();
            var fundamentalsData = new List<FundamentalData>();

            foreach (var stock in stocks)
            {
                var fundamentals = await _dataService.GetFundamentalDataAsync(stock.Symbol);
                fundamentalsData.Add(fundamentals);
                var score = _scoringService.CalculateScore(fundamentals);
                scores.Add(score);
            }

            var recommendations = _allocationService.CalculateInvestmentAllocations(
                scores,
                DateTime.UtcNow.Date);

            return Ok(new DashboardData
            {
                Stocks = stocks,
                Scores = scores,
                FundamentalsData = fundamentalsData,
                Recommendations = recommendations
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error retrieving dashboard data: {ex.Message}");
        }
    }
}

public class DashboardData
{
    public IEnumerable<Stock>? Stocks { get; set; }
    public IEnumerable<StockScore>? Scores { get; set; }
    public IEnumerable<FundamentalData>? FundamentalsData { get; set; }
    public IEnumerable<InvestmentRecommendation>? Recommendations { get; set; }
}