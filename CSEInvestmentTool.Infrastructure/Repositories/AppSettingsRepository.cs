using CSEInvestmentTool.Application.Interfaces;
using CSEInvestmentTool.Domain.Models;
using CSEInvestmentTool.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;

namespace CSEInvestmentTool.Infrastructure.Repositories;

public class AppSettingsRepository : IAppSettingsRepository
{
    private readonly ApplicationDbContext _context;

    public AppSettingsRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<AppSetting>> GetAllSettingsAsync()
    {
        return await _context.AppSettings.OrderBy(s => s.Key).ToListAsync();
    }

    public async Task<string?> GetSettingValueAsync(string key)
    {
        var setting = await _context.AppSettings.FirstOrDefaultAsync(s => s.Key == key);
        return setting?.Value;
    }

    public async Task<T?> GetSettingValueAsync<T>(string key, T defaultValue)
    {
        var strValue = await GetSettingValueAsync(key);

        if (string.IsNullOrEmpty(strValue))
        {
            return defaultValue;
        }

        try
        {
            var converter = TypeDescriptor.GetConverter(typeof(T));
            if (converter != null && converter.CanConvertFrom(typeof(string)))
            {
                return (T)converter.ConvertFromString(strValue)!;
            }
        }
        catch (Exception)
        {
            // If conversion fails, return default
            return defaultValue;
        }

        return defaultValue;
    }

    public async Task<bool> UpdateSettingAsync(string key, string value, string? description = null)
    {
        try
        {
            var setting = await _context.AppSettings.FirstOrDefaultAsync(s => s.Key == key);

            if (setting == null)
            {
                // Create new setting
                setting = new AppSetting
                {
                    Key = key,
                    Value = value,
                    Description = description,
                    LastUpdated = DateTime.UtcNow
                };
                await _context.AppSettings.AddAsync(setting);
            }
            else
            {
                // Update existing setting
                setting.Value = value;
                if (!string.IsNullOrEmpty(description))
                {
                    setting.Description = description;
                }
                setting.LastUpdated = DateTime.UtcNow;
                _context.AppSettings.Update(setting);
            }

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}