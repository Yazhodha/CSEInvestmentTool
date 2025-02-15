using System.ComponentModel.DataAnnotations;

namespace CSEInvestmentTool.Domain.Models;

public class Stock
{
    [Key]
    public int StockId { get; set; }
    
    [Required]
    [MaxLength(10)]
    public string Symbol { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string CompanyName { get; set; } = string.Empty;
    
    [MaxLength(50)]
    public string? Sector { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}
