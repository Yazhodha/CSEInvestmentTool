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

    public async Task<FundamentalData?> GetLatestFundamentalDataForStockAsync(int stockId)
    {
        return await _context.FundamentalData
            .Where(f => f.StockId == stockId)
            .OrderByDescending(f => f.Date)
            .FirstOrDefaultAsync();
    }

    public async Task AddFundamentalDataAsync(FundamentalData data)
    {
        await _context.FundamentalData.AddAsync(data);
        await _context.SaveChangesAsync();
    }
}