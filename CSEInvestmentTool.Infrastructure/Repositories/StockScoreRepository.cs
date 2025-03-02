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
            .Where(s => s.ScoreDate == latestDate && s.Stock!.IsActive)
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
        try
        {
            // Always use score date as the current date to avoid conflicts
            score.ScoreDate = DateTime.UtcNow.Date;

            // Check if a score exists for this stock and date
            var existingScore = await _context.StockScores
                .FirstOrDefaultAsync(s => s.StockId == score.StockId && s.ScoreDate.Date == score.ScoreDate.Date);

            if (existingScore != null)
            {
                // Handle entity tracking
                var localEntry = _context.StockScores
                    .Local
                    .FirstOrDefault(s => s.ScoreId == existingScore.ScoreId);

                if (localEntry != null)
                {
                    _context.Entry(localEntry).State = EntityState.Detached;
                }

                // Update existing score
                existingScore.PEScore = score.PEScore;
                existingScore.ROEScore = score.ROEScore;
                existingScore.DividendYieldScore = score.DividendYieldScore;
                existingScore.DebtEquityScore = score.DebtEquityScore;
                existingScore.ProfitMarginScore = score.ProfitMarginScore;
                existingScore.TotalScore = score.TotalScore;
                existingScore.Rank = score.Rank;
                existingScore.LastUpdated = DateTime.UtcNow;

                _context.Entry(existingScore).State = EntityState.Modified;
            }
            else
            {
                // Make sure we don't have an ID set for a new score
                score.ScoreId = 0;

                // Add new score
                await _context.StockScores.AddAsync(score);
            }

            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error adding stock score for stock {score.StockId}: {ex.Message}", ex);
        }
    }
}