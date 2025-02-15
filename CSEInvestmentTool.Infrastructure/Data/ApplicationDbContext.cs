using Microsoft.EntityFrameworkCore;
using CSEInvestmentTool.Domain.Models;

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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure PostgreSQL-specific settings
        modelBuilder.Entity<Stock>(entity =>
        {
            entity.ToTable("stocks");
            entity.HasKey(e => e.StockId);
            entity.Property(e => e.Symbol).HasMaxLength(10);
            entity.Property(e => e.CompanyName).HasMaxLength(100);
            entity.Property(e => e.Sector).HasMaxLength(50);
        });

        modelBuilder.Entity<FundamentalData>(entity =>
        {
            entity.ToTable("fundamental_data");
            entity.HasKey(e => e.FundamentalId);
            entity.HasOne(d => d.Stock)
                  .WithMany()
                  .HasForeignKey(d => d.StockId);
        });

        modelBuilder.Entity<StockScore>(entity =>
        {
            entity.ToTable("stock_scores");
            entity.HasKey(e => e.ScoreId);
            entity.HasOne(d => d.Stock)
                  .WithMany()
                  .HasForeignKey(d => d.StockId);
        });

        modelBuilder.Entity<InvestmentRecommendation>(entity =>
        {
            entity.ToTable("investment_recommendations");
            entity.HasKey(e => e.RecommendationId);
            entity.HasOne(d => d.Stock)
                  .WithMany()
                  .HasForeignKey(d => d.StockId);
        });
    }
}