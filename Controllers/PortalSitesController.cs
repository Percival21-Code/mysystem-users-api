using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using mysystem_user_api.Models.Portal;
using mysystem_user_api.Services.Interfaces;

namespace mysystem_user_api.Controllers;

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