using mysystem_bff.Models.Admin;
using mysystem_bff.Models.Portal;

namespace mysystem_bff.Services.Interfaces
{
    public interface IMiddlewareCallActionsService
    {
        Task<ServiceResult<PortalCallActionsResponse>> GetCallActions(
            PortalCallActionsQuery query,
            CancellationToken ct = default);
    }
}
