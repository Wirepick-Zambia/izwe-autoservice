namespace IzweAutoService.Application.DTOs;

public record SettingsDto(Dictionary<string, string> Settings);

public record UpdateSettingsRequest(string Category, Dictionary<string, string> Settings);
