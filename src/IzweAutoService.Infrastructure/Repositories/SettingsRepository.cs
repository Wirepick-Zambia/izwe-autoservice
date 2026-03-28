using IzweAutoService.Domain.Entities;
using IzweAutoService.Domain.Interfaces;
using IzweAutoService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace IzweAutoService.Infrastructure.Repositories;

public class SettingsRepository : ISettingsRepository
{
    private readonly AppDbContext _db;

    public SettingsRepository(AppDbContext db) => _db = db;

    public async Task<List<AppSetting>> GetAllAsync()
        => await _db.AppSettings.OrderBy(s => s.Category).ThenBy(s => s.Key).ToListAsync();

    public async Task<List<AppSetting>> GetByCategoryAsync(string category)
        => await _db.AppSettings.Where(s => s.Category == category).ToListAsync();

    public async Task<string?> GetValueAsync(string key)
        => (await _db.AppSettings.FirstOrDefaultAsync(s => s.Key == key))?.Value;

    public async Task SetValueAsync(string key, string value, string category)
    {
        var setting = await _db.AppSettings.FirstOrDefaultAsync(s => s.Key == key);
        if (setting is null)
        {
            _db.AppSettings.Add(new AppSetting { Key = key, Value = value, Category = category });
        }
        else
        {
            setting.Value = value;
        }
        await _db.SaveChangesAsync();
    }

    public async Task SetBulkAsync(Dictionary<string, string> settings, string category)
    {
        var keys = settings.Keys.ToList();
        var existing = await _db.AppSettings
            .Where(s => keys.Contains(s.Key))
            .ToDictionaryAsync(s => s.Key);

        foreach (var (key, value) in settings)
        {
            if (existing.TryGetValue(key, out var setting))
            {
                setting.Value = value;
                setting.Category = category;
            }
            else
            {
                _db.AppSettings.Add(new AppSetting { Key = key, Value = value, Category = category });
            }
        }
        await _db.SaveChangesAsync();
    }
}
