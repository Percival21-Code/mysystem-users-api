using mysystem_bff.Models.Admin;
using mysystem_bff.Models.Portal;

namespace mysystem_bff.Services.Interfaces
{
    public interface IMiddlewareSiteSystemsService
    {
        // =====================================================
        // single-site flow
        // =====================================================

        Task<ServiceResult<PortalSiteSystemsResponse>>
            GetSiteSystemsAsync(
                PortalSiteSystemsQuery query,
                CancellationToken ct = default);

        // =====================================================
        // Dashboard multi-site flow
        // =====================================================

        Task<ServiceResult<List<PortalSiteSystemDto>>>
            GetSiteSystemsForSitesAsync(
                IReadOnlyCollection<string> siteIds,
                string maintainedYN,
                CancellationToken ct = default);
    }
}