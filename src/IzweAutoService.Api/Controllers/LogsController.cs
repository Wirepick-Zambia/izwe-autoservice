using IzweAutoService.Application.DTOs;
using IzweAutoService.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace IzweAutoService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LogsController : ControllerBase
{
    private readonly IProcessingLogRepository _repo;

    public LogsController(IProcessingLogRepository repo) => _repo = repo;

    [HttpGet]
    public async Task<IActionResult> GetRecent([FromQuery] int count = 20)
    {
        var logs = await _repo.GetRecentAsync(count);
        return Ok(logs.Select(l => new ProcessingLogDto(
            l.Id, l.StartedAt, l.CompletedAt, l.TotalFound, l.TotalSent, l.TotalFailed, l.ErrorMessage
        )));
    }
}
