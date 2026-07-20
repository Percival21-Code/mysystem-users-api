using mysystem_bff.Models.Admin;
using mysystem_bff.Models.Portal;

namespace mysystem_bff.Services.Interfaces;

public interface IMiddlewareSitesService
{
    Task<ServiceResult<PortalSitesResponse>> GetSites(
        PortalSitesQuery query,
        CancellationToken cancellationToken = default);
}