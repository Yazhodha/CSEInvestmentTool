using CSEInvestmentTool.Application.Interfaces;
using CSEInvestmentTool.Domain.Models;
using CSEInvestmentTool.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CSEInvestmentTool.Infrastructure.Repositories;

public class FundamentalDataRepository : IFundamentalDataRepository
{
    private readonly ApplicationDbContext _context;

    public FundamentalDataRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<FundamentalData>> GetFundamentalDataForStockAsync(int stockId)
    {
        return await _context.FundamentalData
            .Where(f => f.StockId == stockId)
            .OrderByDescending(f => f.Date)
            .ToListAsync();
    }

    public async Task<List<FundamentalData>> GetLatestFundamentalDataForStocksAsync(IEnumerable<int> stockIds)
    {
        return await _context.FundamentalData
            .Where(f => stockIds.Contains(f.StockId))
            .GroupBy(f => f.StockId)
            .Select(g => g.OrderByDescending(f => f.Date).First())
            .ToListAsync();
    }

    public async Task<FundamentalData?> GetLatestFundamentalDataForStockAsync(int stockId)
    {
        return await _context.FundamentalData
            .Where(f => f.StockId == stockId)
            .OrderByDescending(f => f.Date)
            .FirstOrDefaultAsync();
    }

    public async Task AddFundamentalDataAsync(FundamentalData data)
    {
        try
        {
            // If an ID is specified (existing entity), make sure we handle tracking properly
            if (data.FundamentalId > 0)
            {
                // If this is an existing entity, make sure it's not tracked
                var localEntry = _context.FundamentalData
                    .Local
                    .FirstOrDefault(f => f.FundamentalId == data.FundamentalId);

                if (localEntry != null)
                {
                    _context.Entry(localEntry).State = EntityState.Detached;
                }

                // Use Update instead of Add
                _context.Entry(data).State = EntityState.Modified;
            }
            else
            {
                // Check if fundamental data exists for this stock and date
                var existingData = await _context.FundamentalData
                    .FirstOrDefaultAsync(f => f.StockId == data.StockId && f.Date.Date == data.Date.Date);

                if (existingData != null)
                {
                    // Update existing fundamental data
                    existingData.MarketPrice = data.MarketPrice;
                    existingData.NAV = data.NAV;
                    existingData.EPS = data.EPS;
                    existingData.AnnualDividend = data.AnnualDividend;
                    existingData.TotalLiabilities = data.TotalLiabilities;
                    existingData.TotalEquity = data.TotalEquity;
                    existingData.LastUpdated = DateTime.UtcNow;

                    _context.FundamentalData.Update(existingData);
                }
                else
                {
                    // Add new fundamental data - make sure FundamentalId is 0
                    // so EF will assign a new ID
                    data.FundamentalId = 0;
                    await _context.FundamentalData.AddAsync(data);
                }
            }

            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error adding fundamental data for stock {data.StockId}: {ex.Message}", ex);
        }
    }
}