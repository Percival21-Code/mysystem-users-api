using mysystem_bff.Models.Admin;
using mysystem_bff.Models.Portal.DashboardData;
using mysystem_bff.Services.Interfaces;

namespace mysystem_bff.Services.Services;

public class DashboardDataService : IDashboardDataService
{
    private readonly ICallsDashboardService _callsDashboardService;
    private readonly IMaintenanceDashboardService _maintenanceDashboardService;

    public DashboardDataService(
        ICallsDashboardService callsDashboardService,
        IMaintenanceDashboardService maintenanceDashboardService)
    {
        _callsDashboardService = callsDashboardService;
        _maintenanceDashboardService = maintenanceDashboardService;
    }

    // =========================================================
    // Calls dashboard
    // =========================================================

    public Task<ServiceResult<PortalCallsDashboardDataDto>>
        GetCallsDashboardDataAsync(
            PortalDashboardDataQuery query,
            CancellationToken ct = default)
    {
        return _callsDashboardService.GetDashboardDataAsync(
            query,
            ct);
    }

    public Task<ServiceResult<PortalDashboardCallsItemsResponse>>
        GetCallsDashboardItemsAsync(
            PortalDashboardCallsItemsQuery query,
            CancellationToken ct = default)
    {
        return _callsDashboardService.GetDashboardItemsAsync(
            query,
            ct);
    }

    // =========================================================
    // Maintenance dashboard
    // =========================================================

    public Task<ServiceResult<PortalMaintenanceDashboardDataDto>>
        GetMaintenanceDashboardDataAsync(
            PortalDashboardDataQuery query,
            CancellationToken ct = default)
    {
        return _maintenanceDashboardService.GetDashboardDataAsync(
            query,
            ct);
    }

    public Task<ServiceResult<PortalDashboardMaintenanceItemsResponse>>
        GetMaintenanceDashboardItemsAsync(
            PortalDashboardMaintenanceItemsQuery query,
            CancellationToken ct = default)
    {
        return _maintenanceDashboardService.GetDashboardItemsAsync(
            query,
            ct);
    }
}