namespace mysystem_bff.Services.Interfaces;

using mysystem_bff.Models.Admin;
using mysystem_bff.Models.Portal;

public interface IMiddlewareCallsService
{
    Task<ServiceResult<PortalCallsResponse>> GetCalls(
        PortalCallsQuery query,
        CancellationToken cancellationToken = default);
}
