using IzweAutoService.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace IzweAutoService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly DashboardService _service;

    public DashboardController(DashboardService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> Get() => Ok(await _service.GetDashboardAsync());
}
