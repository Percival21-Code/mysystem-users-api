using Dapper;
using MySqlConnector;
using mysystem_user_api.Models.Admin;
using mysystem_user_api.Services.Interfaces;

namespace mysystem_user_api.Services.Services;

public class AdminRoleService : IAdminRoleService
{
    private readonly MySqlConnection _db;

    public AdminRoleService(MySqlConnection db)
    {
        _db = db;
    }

    public async Task<List<RoleDto>> GetRoles()
    {
        var roles = await _db.QueryAsync<RoleDto>(
            """
            SELECT
                role_id AS RoleId,
                role_name AS RoleName
            FROM roles
            ORDER BY role_id ASC;
            """
        );

        return roles.ToList();
    }
}