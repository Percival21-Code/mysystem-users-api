using mysystem_user_api.Models.Admin;

namespace mysystem_user_api.Services.Interfaces;

public interface IAdminUserPasswordService
{
    Task<ServiceResult<object>> ResetPassword(string userId, ResetPasswordRequest request);
}