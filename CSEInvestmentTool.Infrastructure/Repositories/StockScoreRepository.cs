using CSEInvestmentTool.Application.Interfaces;
using CSEInvestmentTool.Domain.Models;
using CSEInvestmentTool.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CSEInvestmentTool.Infrastructure.Repositories;

public class StockScoreRepository : IStockScoreRepository
{
    private readonly ApplicationDbContext _context;

    public StockScoreRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<StockScore>> GetLatestScoresAsync()
    {
        var latestDate = await _context.StockScores
            .MaxAsync(s => s.ScoreDate);

        return await _context.StockScores
            .Where(s => s.ScoreDate == latestDate)
            .Include(s => s.Stock)
            .OrderByDescending(s => s.TotalScore)
            .ToListAsync();
    }

    public async Task<StockScore?> GetLatestScoreForStockAsync(int stockId)
    {
        return await _context.StockScores
            .Where(s => s.StockId == stockId)
            .OrderByDescending(s => s.ScoreDate)
            .FirstOrDefaultAsync();
    }

    public async Task AddStockScoreAsync(StockScore score)
    {
        await _context.StockScores.AddAsync(score);
        await _context.SaveChangesAsync();
    }
}
