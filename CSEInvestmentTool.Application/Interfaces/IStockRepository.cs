using CSEInvestmentTool.Domain.Models;

namespace CSEInvestmentTool.Application.Interfaces;

public interface IStockRepository
{
    Task<IEnumerable<Stock>> GetAllStocksAsync();
    Task<Stock?> GetStockByIdAsync(int id);
    Task<Stock?> GetStockBySymbolAsync(string symbol);
    Task AddStockAsync(Stock stock);
    Task UpdateStockAsync(Stock stock);
    Task DeleteStockAsync(int id);
}
