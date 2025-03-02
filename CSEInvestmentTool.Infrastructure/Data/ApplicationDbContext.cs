using CSEInvestmentTool.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace CSEInvestmentTool.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Stock> Stocks { get; set; }
    public DbSet<FundamentalData> FundamentalData { get; set; }
    public DbSet<StockScore> StockScores { get; set; }
    public DbSet<InvestmentRecommendation> InvestmentRecommendations { get; set; }
    public DbSet<AppSetting> AppSettings { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure unique constraints and indexes
        modelBuilder.Entity<Stock>()
            .HasIndex(s => s.Symbol)
            .IsUnique();

        modelBuilder.Entity<FundamentalData>()
            .HasIndex(f => new { f.StockId, f.Date })
            .IsUnique();

        modelBuilder.Entity<StockScore>()
            .HasIndex(s => new { s.StockId, s.ScoreDate })
            .IsUnique();

        modelBuilder.Entity<InvestmentRecommendation>()
            .HasIndex(r => new { r.StockId, r.RecommendationDate })
            .IsUnique();

        modelBuilder.Entity<AppSetting>()
            .HasIndex(s => s.Key)
            .IsUnique();
    }
}