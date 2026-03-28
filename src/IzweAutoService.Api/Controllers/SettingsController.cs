using IzweAutoService.Application.DTOs;
using IzweAutoService.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace IzweAutoService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SettingsController : ControllerBase
{
    private readonly SettingsService _service;

    public SettingsController(SettingsService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await _service.GetAllAsync());

    [HttpGet("{category}")]
    public async Task<IActionResult> GetByCategory(string category)
        => Ok(await _service.GetByCategoryAsync(category));

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateSettingsRequest request)
    {
        await _service.UpdateAsync(request);
        return Ok(new { message = "Settings updated" });
    }
}
