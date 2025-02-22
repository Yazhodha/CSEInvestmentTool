using CSEInvestmentTool.Application.Interfaces;
using CSEInvestmentTool.Domain.Models;
using CSEInvestmentTool.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CSEInvestmentTool.Infrastructure.Repositories;

public class InvestmentRecommendationRepository : IInvestmentRecommendationRepository
{
    private readonly ApplicationDbContext _context;

    public InvestmentRecommendationRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> HasRecommendationForStockOnDateAsync(int stockId, DateTime date)
    {
        return await _context.InvestmentRecommendations
            .AnyAsync(r => r.StockId == stockId && r.RecommendationDate.Date == date.Date);
    }

    public async Task<IEnumerable<InvestmentRecommendation>> GetLatestRecommendationsAsync()
    {
        var latestDate = await _context.InvestmentRecommendations
            .MaxAsync(r => r.RecommendationDate);

        return await _context.InvestmentRecommendations
            .Where(r => r.RecommendationDate == latestDate && r.Stock!.IsActive)
            .Include(r => r.Stock)
            .OrderByDescending(r => r.RecommendedAmount)
            .ToListAsync();
    }

    public async Task AddRecommendationAsync(InvestmentRecommendation recommendation)
    {
        // Check if recommendation exists
        var hasExisting = await HasRecommendationForStockOnDateAsync(
            recommendation.StockId,
            recommendation.RecommendationDate);

        if (hasExisting)
        {
            // Update existing recommendation
            var existing = await _context.InvestmentRecommendations
                .FirstOrDefaultAsync(r =>
                    r.StockId == recommendation.StockId &&
                    r.RecommendationDate.Date == recommendation.RecommendationDate.Date);

            if (existing != null)
            {
                existing.RecommendedAmount = recommendation.RecommendedAmount;
                existing.RecommendationReason = recommendation.RecommendationReason;
                existing.LastUpdated = DateTime.UtcNow;
                _context.InvestmentRecommendations.Update(existing);
            }
        }
        else
        {
            // Add new recommendation
            await _context.InvestmentRecommendations.AddAsync(recommendation);
        }

        await _context.SaveChangesAsync();
    }
}
