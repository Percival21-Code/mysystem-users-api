using mysystem_user_api.Models.Admin;

namespace mysystem_user_api.Services.Interfaces;

public interface IAdminUserCreateService
{
    Task<ServiceResult<UserListItemDto>> CreateUser(CreateUserRequest request);
}
