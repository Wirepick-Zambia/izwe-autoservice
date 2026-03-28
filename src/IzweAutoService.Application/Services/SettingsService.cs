using IzweAutoService.Application.DTOs;
using IzweAutoService.Domain.Interfaces;

namespace IzweAutoService.Application.Services;

public class SettingsService
{
    private readonly ISettingsRepository _repo;

    public SettingsService(ISettingsRepository repo) => _repo = repo;

    public async Task<SettingsDto> GetAllAsync()
    {
        var settings = await _repo.GetAllAsync();
        return new SettingsDto(settings.ToDictionary(s => s.Key, s => s.Value));
    }

    public async Task<SettingsDto> GetByCategoryAsync(string category)
    {
        var settings = await _repo.GetByCategoryAsync(category);
        return new SettingsDto(settings.ToDictionary(s => s.Key, s => s.Value));
    }

    public async Task UpdateAsync(UpdateSettingsRequest request)
    {
        await _repo.SetBulkAsync(request.Settings, request.Category);
    }

    public async Task<string?> GetValueAsync(string key) => await _repo.GetValueAsync(key);
}
