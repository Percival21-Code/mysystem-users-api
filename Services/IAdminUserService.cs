using mysystem_user_api.Models.Admin;

namespace mysystem_user_api.Services;

public interface IAdminUserService
{
    Task<List<UserListItemDto>> GetUsers(UserFilters filters);
    Task<UserListItemDto?> GetUserById(string userId);
    Task<ServiceResult<UserListItemDto>> CreateUser(CreateUserRequest request);
}