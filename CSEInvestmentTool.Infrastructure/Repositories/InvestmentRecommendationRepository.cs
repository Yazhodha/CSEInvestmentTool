using System;
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

    public async Task<IEnumerable<InvestmentRecommendation>> GetLatestRecommendationsAsync()
    {
        var latestDate = await _context.InvestmentRecommendations
            .MaxAsync(r => r.RecommendationDate);

        return await _context.InvestmentRecommendations
            .Where(r => r.RecommendationDate == latestDate)
            .Include(r => r.Stock)
            .OrderByDescending(r => r.RecommendedAmount)
            .ToListAsync();
    }

    public async Task AddRecommendationAsync(InvestmentRecommendation recommendation)
    {
        await _context.InvestmentRecommendations.AddAsync(recommendation);
        await _context.SaveChangesAsync();
    }
}
