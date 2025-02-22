using CSEInvestmentTool.Application.Interfaces;
using CSEInvestmentTool.Domain.Models;
using CSEInvestmentTool.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CSEInvestmentTool.Infrastructure.Repositories;

public class StockRepository : IStockRepository
{
    private readonly ApplicationDbContext _context;

    public StockRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Stock>> GetAllStocksAsync()
    {
        return await _context.Stocks
            .Where(s => s.IsActive)
            .OrderBy(s => s.Symbol)
            .ToListAsync();
    }

    public async Task<Stock?> GetStockByIdAsync(int id)
    {
        return await _context.Stocks.FindAsync(id);
    }

    public async Task<Stock?> GetStockBySymbolAsync(string symbol)
    {
        return await _context.Stocks
            .FirstOrDefaultAsync(s => s.Symbol == symbol);
    }

    public async Task AddStockAsync(Stock stock)
    {
        await _context.Stocks.AddAsync(stock);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateStockAsync(Stock stock)
    {
        _context.Stocks.Update(stock);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteStockAsync(int id)
    {
        var stock = await _context.Stocks.FindAsync(id);
        if (stock != null)
        {
            stock.IsActive = false;
            stock.LastUpdated = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }
}
