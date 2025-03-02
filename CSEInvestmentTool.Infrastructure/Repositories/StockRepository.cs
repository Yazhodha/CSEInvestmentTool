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

    public async Task<Stock> AddStockAsync(Stock stock)
    {
        // Check for any existing stock with this symbol (case-insensitive)
        var existingStock = await _context.Stocks
            .FirstOrDefaultAsync(s => s.Symbol.ToUpper() == stock.Symbol.ToUpper());

        if (existingStock != null)
        {
            if (!existingStock.IsActive)
            {
                // Reactivate and update the existing stock
                existingStock.IsActive = true;
                existingStock.CompanyName = stock.CompanyName;
                existingStock.Sector = stock.Sector;
                existingStock.LastUpdated = DateTime.UtcNow;
                _context.Stocks.Update(existingStock);
                await _context.SaveChangesAsync();
                return existingStock;
            }
            else
            {
                throw new InvalidOperationException($"Stock with symbol {stock.Symbol} already exists and is active.");
            }
        }

        // Add new stock
        await _context.Stocks.AddAsync(stock);
        await _context.SaveChangesAsync();
        return stock;
    }

    public async Task UpdateStockAsync(Stock stock)
    {
        try
        {
            // Handle entity tracking
            var localEntry = _context.Stocks
                .Local
                .FirstOrDefault(s => s.StockId == stock.StockId);

            if (localEntry != null)
            {
                _context.Entry(localEntry).State = EntityState.Detached;
            }

            _context.Entry(stock).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error updating stock {stock.Symbol}: {ex.Message}", ex);
        }
    }

    public async Task DeleteStockAsync(int id)
    {
        // Find all recommendations for this stock first
        var recommendations = await _context.InvestmentRecommendations
            .Where(r => r.StockId == id)
            .ToListAsync();

        if (recommendations.Any())
        {
            _context.InvestmentRecommendations.RemoveRange(recommendations);
            await _context.SaveChangesAsync();
        }

        // Then handle the stock
        var stock = await _context.Stocks.FindAsync(id);
        if (stock != null)
        {
            stock.IsActive = false;
            stock.LastUpdated = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }
}