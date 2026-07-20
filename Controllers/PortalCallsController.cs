namespace mysystem_bff.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using mysystem_bff.Models.Portal;
using mysystem_bff.Services.Interfaces;

[ApiController]
[Route("api/portal/calls")]
[Authorize]
public class PortalCallsController : ControllerBase
{
    private readonly IMiddlewareCallsService _callsService;

    public PortalCallsController(IMiddlewareCallsService callsService)
    {
        _callsService = callsService;
    }

    [HttpGet]
    public async Task<ActionResult<PortalCallsResponse>> GetCalls(
        [FromQuery] PortalCallsQuery query,
        CancellationToken ct)
    {
        var result = await _callsService.GetCalls(query, ct);

        if (!result.Success)
        {
            return StatusCode(
                result.StatusCode,
                result.Error);
        }

        return Ok(result.Data);
    } 
}