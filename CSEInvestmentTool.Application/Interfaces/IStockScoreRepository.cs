using CSEInvestmentTool.Domain.Models;

namespace CSEInvestmentTool.Application.Interfaces;

public interface IStockScoreRepository
{
    Task<IEnumerable<StockScore>> GetLatestScoresAsync();
    Task<StockScore?> GetLatestScoreForStockAsync(int stockId);
    Task AddStockScoreAsync(StockScore score);
}
