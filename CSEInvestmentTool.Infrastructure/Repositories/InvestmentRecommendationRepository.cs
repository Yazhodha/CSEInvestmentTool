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
        // Try to find the latest date
        var maxDateQuery = _context.InvestmentRecommendations.Select(r => r.RecommendationDate);

        if (!await maxDateQuery.AnyAsync())
        {
            return new List<InvestmentRecommendation>();
        }

        var latestDate = await maxDateQuery.MaxAsync();

        return await _context.InvestmentRecommendations
            .Where(r => r.RecommendationDate.Date == latestDate.Date && r.Stock!.IsActive)
            .Include(r => r.Stock)
            .OrderByDescending(r => r.RecommendedAmount)
            .ToListAsync();
    }

    public async Task AddRecommendationAsync(InvestmentRecommendation recommendation)
    {
        try
        {
            // Check if recommendation exists
            var hasExisting = await HasRecommendationForStockOnDateAsync(
                recommendation.StockId,
                recommendation.RecommendationDate);

            if (hasExisting)
            {
                // Find the existing recommendation
                var existing = await _context.InvestmentRecommendations
                    .FirstOrDefaultAsync(r =>
                        r.StockId == recommendation.StockId &&
                        r.RecommendationDate.Date == recommendation.RecommendationDate.Date);

                if (existing != null)
                {
                    // Handle entity tracking
                    var localEntry = _context.InvestmentRecommendations
                        .Local
                        .FirstOrDefault(r => r.RecommendationId == existing.RecommendationId);

                    if (localEntry != null)
                    {
                        _context.Entry(localEntry).State = EntityState.Detached;
                    }

                    // Update existing recommendation
                    existing.RecommendedAmount = recommendation.RecommendedAmount;
                    existing.RecommendationReason = recommendation.RecommendationReason;
                    existing.LastUpdated = DateTime.UtcNow;

                    _context.Entry(existing).State = EntityState.Modified;
                }
            }
            else
            {
                // Make sure we don't have an ID set for a new recommendation
                recommendation.RecommendationId = 0;

                // Add new recommendation
                await _context.InvestmentRecommendations.AddAsync(recommendation);
            }

            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error adding investment recommendation for stock {recommendation.StockId}: {ex.Message}", ex);
        }
    }
}