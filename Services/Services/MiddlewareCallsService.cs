namespace mysystem_bff.Services.Services;

using mysystem_bff.Models.Admin;
using mysystem_bff.Models.Portal;
using mysystem_bff.Services.Interfaces;

public class MiddlewareCallsService : IMiddlewareCallsService
{
    public Task<ServiceResult<PortalCallsResponse>> GetCalls(
        PortalCallsQuery query,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}