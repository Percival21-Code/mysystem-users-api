using mysystem_bff.Models.Admin;

namespace mysystem_bff.Services.Interfaces;

public interface IMiddlewareAuthService
{
    Task<ServiceResult<string>> GetMiddlewareToken();
}