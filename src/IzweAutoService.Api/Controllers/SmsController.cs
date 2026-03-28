using IzweAutoService.Application.Services;
using IzweAutoService.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace IzweAutoService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SmsController : ControllerBase
{
    private readonly SmsQueryService _queryService;
    private readonly SmsProcessingService _processingService;

    public SmsController(SmsQueryService queryService, SmsProcessingService processingService)
    {
        _queryService = queryService;
        _processingService = processingService;
    }

    [HttpGet]
    public async Task<IActionResult> GetPaged(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] SmsStatus? status = null,
        [FromQuery] string? country = null,
        [FromQuery] string? search = null)
    {
        var result = await _queryService.GetPagedAsync(page, pageSize, status, country, search);
        return Ok(result);
    }

    [HttpPost("process")]
    public async Task<IActionResult> TriggerProcessing()
    {
        await _processingService.ProcessAsync();
        return Ok(new { message = "Processing completed" });
    }
}
