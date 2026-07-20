using mysystem_bff.Models.Admin;

namespace mysystem_bff.Services.Interfaces;

public interface IAdminUserUpdateService
{
    Task<ServiceResult<UserListItemDto>> UpdateUser(string userId, UpdateUserRequest request);
}