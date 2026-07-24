using mysystem_bff.Models.Admin;
using mysystem_bff.Models.Middleware;
using mysystem_bff.Models.Portal;

namespace mysystem_bff.Services.Interfaces
{
    public interface IMiddlewareSiteSystemsService
    {
        Task<ServiceResult<MiddlewareSiteSystemsResponse>> GetSiteSystemsAsync(
            PortalSiteSystemsQuery query,
            CancellationToken ct = default);
    }
}
