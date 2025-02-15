using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CSEInvestmentTool.Domain.Models;

public class StockScore
{
    [Key]
    public int ScoreId { get; set; }
    
    public int StockId { get; set; }
    
    [Required]
    public DateTime ScoreDate { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal PEScore { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal ROEScore { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal DividendYieldScore { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal DebtEquityScore { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal ProfitMarginScore { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalScore { get; set; }
    
    public int Rank { get; set; }
    
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    
    [ForeignKey("StockId")]
    public virtual Stock? Stock { get; set; }
}