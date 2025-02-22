using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CSEInvestmentTool.Domain.Models;

public class FundamentalData
{
    [Key]
    public int FundamentalId { get; set; }

    public int StockId { get; set; }

    [Required]
    public DateTime Date { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    [Required]
    public decimal MarketPrice { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    [Required]
    public decimal NAV { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    [Required]
    public decimal EPS { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal AnnualDividend { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    [Required]
    public decimal TotalLiabilities { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    [Required]
    public decimal TotalEquity { get; set; }

    // Calculated fields
    [NotMapped]
    public decimal? PERatio => MarketPrice / (EPS != 0 ? EPS : 1);

    [NotMapped]
    public decimal? ROE => (EPS * 100) / (NAV != 0 ? NAV : 1);

    [NotMapped]
    public decimal? DividendYield => (AnnualDividend * 100) / (MarketPrice != 0 ? MarketPrice : 1);

    [NotMapped]
    public decimal? DebtToEquityRatio => TotalLiabilities / (TotalEquity != 0 ? TotalEquity : 1);

    [NotMapped]
    public decimal? PBV => NAV > 0 ? MarketPrice / NAV : null;

    [NotMapped]
    public decimal? Return => MarketPrice > 0 ? (EPS * 100 / MarketPrice) : null;

    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    [ForeignKey("StockId")]
    public virtual Stock? Stock { get; set; }
}