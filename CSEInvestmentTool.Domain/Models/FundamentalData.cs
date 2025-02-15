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
    public decimal? PERatio { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal? ROE { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal? DividendYield { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal? DebtToEquityRatio { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal? NetProfitMargin { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal? MarketCap { get; set; }
    
    public long? TradingVolume { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal? HighPrice { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal? LowPrice { get; set; }
    
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    
    [ForeignKey("StockId")]
    public virtual Stock? Stock { get; set; }
}