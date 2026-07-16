using mysystem_user_api.Models.Admin;
using mysystem_user_api.Models.Portal;

namespace mysystem_user_api.Services.Interfaces;

public interface IMiddlewareSitesService
{
    Task<ServiceResult<PortalSitesResponse>> GetSites(
        PortalSitesQuery query,
        CancellationToken cancellationToken = default);
}