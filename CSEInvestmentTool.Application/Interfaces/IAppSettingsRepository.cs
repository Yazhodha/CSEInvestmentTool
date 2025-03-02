using CSEInvestmentTool.Domain.Models;

namespace CSEInvestmentTool.Application.Interfaces;

public interface IAppSettingsRepository
{
    Task<string?> GetSettingValueAsync(string key);
    Task<T?> GetSettingValueAsync<T>(string key, T defaultValue);
    Task<bool> UpdateSettingAsync(string key, string value, string? description = null);
    Task<IEnumerable<AppSetting>> GetAllSettingsAsync();
}