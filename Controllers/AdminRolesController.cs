using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using mysystem_user_api.Models.Admin;
using mysystem_user_api.Services.Interfaces;

namespace mysystem_user_api.Controllers;

[ApiController]
[Route("api/admin/roles")]
[Authorize(Policy = "AdministratorOnly")]
public class AdminRolesController : ControllerBase
{
    private readonly IAdminRoleService _roleService;

    public AdminRolesController(IAdminRoleService roleService)
    {
        _roleService = roleService;
    }

    [HttpGet]
    public async Task<ActionResult<List<RoleDto>>> GetRoles()
    {
        var roles = await _roleService.GetRoles();
        return Ok(roles);
    }
}