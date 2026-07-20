namespace mysystem_bff.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using mysystem_bff.Models.Portal;
using mysystem_bff.Services.Interfaces;

[ApiController]
[Route("api/portal/sites")]
[Authorize]
public class PortalSitesController : ControllerBase
{
    private readonly IMiddlewareSitesService _sitesService;

    public PortalSitesController(
        IMiddlewareSitesService sitesService)
    {
        _sitesService = sitesService;
    }

    [HttpGet]
    public async Task<ActionResult<PortalSitesResponse>> GetSites(
        [FromQuery] PortalSitesQuery query,
        CancellationToken cancellationToken)
    {
        var result = await _sitesService.GetSites(
            query,
            cancellationToken);

        if (!result.Success)
        {
            return StatusCode(
                result.StatusCode,
                result.Error);
        }

        return Ok(result.Data);
    }
}