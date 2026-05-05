using mysystem_user_api.Models.Admin;

namespace mysystem_user_api.Services.Interfaces;

public interface IAdminUserStatusService
{
    Task<ServiceResult<object>> UpdateUserStatus(string userId, UpdateUserStatusRequest request);
}
