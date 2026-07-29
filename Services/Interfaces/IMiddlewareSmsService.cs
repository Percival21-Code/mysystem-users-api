using mysystem_bff.Models.Admin;
using mysystem_bff.Models.Portal;

namespace mysystem_bff.Services.Interfaces
{
    public interface IMiddlewareSmsService
    {
        Task<ServiceResult<PortalSMSResponse>> GetSms(
            PortalSMSQuery query,
            CancellationToken ct);
    }
}
