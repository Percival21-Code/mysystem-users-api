using mysystem_user_api.Models.Admin;

namespace mysystem_user_api.Services.Interfaces;

public interface IAdminUserReadService
{
    Task<List<UserListItemDto>> GetUsers(UserFilters filters);
    Task<UserListItemDto?> GetUserById(string userId);
}
