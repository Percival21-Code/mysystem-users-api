using mysystem_user_api.Models.Admin;

namespace mysystem_user_api.Services.Interfaces;

public interface IAdminRoleService
{
    Task<List<RoleDto>> GetRoles();
}