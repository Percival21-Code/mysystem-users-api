using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using mysystem_bff.Models.Portal.DashboardData;
using mysystem_bff.Services.Interfaces;

namespace mysystem_bff.Controllers;

[ApiController]
[Authorize]
[Route("api/portal/dashboard")]
public class DashboardDataController : ControllerBase
{
    private readonly IDashboardDataService _dashboardService;
    private readonly IPortalAccessService _accessService;

    public DashboardDataController(
        IDashboardDataService dashboardService,
        IPortalAccessService accessService)
    {
        _dashboardService = dashboardService;
        _accessService = accessService;
    }

    [HttpGet("calls-dashboard")]
    public async Task<ActionResult<PortalCallsDashboardDataDto>>
        GetCallsDashboardDataAsync(
            [FromQuery] PortalDashboardDataQuery query,
            CancellationToken ct)
    {
        query.Board = DashboardBoardType.CALLS_BOARD;

        var customerNo =
            query.CustomerNo?.Trim().ToUpperInvariant() ?? "";

        var siteId =
            query.SiteId?.Trim().ToUpperInvariant() ?? "";

        if (string.IsNullOrWhiteSpace(customerNo) &&
            string.IsNullOrWhiteSpace(siteId))
        {
            return BadRequest(
                "Either Customer No or Site ID is required.");
        }

        if (!_accessService.HasUnrestrictedAccess(User))
        {
            if (!string.IsNullOrWhiteSpace(siteId))
            {
                var canAccessSite =
                    await _accessService.CanAccessSite(
                        User,
                        siteId,
                        ct);

                if (!canAccessSite)
                    return Forbid();
            }
            else
            {
                var canAccessCustomer =
                    await _accessService.CanAccessCustomer(
                        User,
                        customerNo,
                        ct);

                if (!canAccessCustomer)
                    return Forbid();
            }
        }

        query.CustomerNo = customerNo;
        query.SiteId = siteId;

        var result =
            await _dashboardService.GetCallsDashboardDataAsync(
                query,
                ct);

        if (!result.Success)
        {
            return StatusCode(
                result.StatusCode,
                result.Error);
        }

        return Ok(result.Data);
    }
}