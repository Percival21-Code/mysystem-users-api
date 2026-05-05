using mysystem_user_api.Models.Admin;

namespace mysystem_user_api.Services.Interfaces;

public interface IAdminUserUpdateService
{
    Task<ServiceResult<UserListItemDto>> UpdateUser(string userId, UpdateUserRequest request);
}