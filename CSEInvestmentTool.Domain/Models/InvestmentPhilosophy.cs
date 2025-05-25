using System.ComponentModel.DataAnnotations;

namespace CSEInvestmentTool.Domain.Models;

public class InvestmentPhilosophy
{
    [Key]
    public int PhilosophyId { get; set; }

    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public string PromptTemplate { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}
