using mysystem_bff.Models.Admin;
using mysystem_bff.Models.Portal.DashboardData;

namespace mysystem_bff.Services.Interfaces;

public interface IDashboardDataService
{
    Task<ServiceResult<PortalCallsDashboardDataDto>>
        GetCallsDashboardDataAsync(
            PortalDashboardDataQuery query,
            CancellationToken ct = default);

    Task<ServiceResult<PortalDashboardCallsItemsResponse>>
        GetCallsDashboardItemsAsync(
            PortalDashboardCallsItemsQuery query,
            CancellationToken ct = default);
}