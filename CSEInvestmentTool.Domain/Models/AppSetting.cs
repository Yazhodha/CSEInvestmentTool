using System.ComponentModel.DataAnnotations;

namespace CSEInvestmentTool.Domain.Models;

public class AppSetting
{
    [Key]
    public int SettingId { get; set; }

    [Required]
    [MaxLength(50)]
    public string Key { get; set; } = string.Empty;

    [Required]
    public string Value { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Description { get; set; }

    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}