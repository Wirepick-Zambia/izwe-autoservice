using IzweAutoService.Domain.Entities;

namespace IzweAutoService.Domain.Interfaces;

public interface ISettingsRepository
{
    Task<List<AppSetting>> GetAllAsync();
    Task<List<AppSetting>> GetByCategoryAsync(string category);
    Task<string?> GetValueAsync(string key);
    Task SetValueAsync(string key, string value, string category);
    Task SetBulkAsync(Dictionary<string, string> settings, string category);
}
