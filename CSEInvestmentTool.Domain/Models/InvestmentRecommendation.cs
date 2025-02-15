using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CSEInvestmentTool.Domain.Models;

public class InvestmentRecommendation
{
    [Key]
    public int RecommendationId { get; set; }
    
    public int StockId { get; set; }
    
    [Required]
    public DateTime RecommendationDate { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal RecommendedAmount { get; set; }
    
    public string? RecommendationReason { get; set; }
    
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    
    [ForeignKey("StockId")]
    public virtual Stock? Stock { get; set; }
}