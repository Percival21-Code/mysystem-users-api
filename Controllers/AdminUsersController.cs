using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using mysystem_user_api.Models.Admin;
using mysystem_user_api.Services;

namespace mysystem_user_api.Controllers;

[ApiController]
[Route("api/admin/users")]
[Authorize(Policy = "AdministratorOnly")]
public class AdminUsersController : ControllerBase
{
    private readonly IAdminUserService _adminUserService;

    public AdminUsersController(IAdminUserService adminUserService)
    {
        _adminUserService = adminUserService;
    }

    // get all users
    [HttpGet]
    public async Task<ActionResult<List<UserListItemDto>>> GetUsers([FromQuery] UserFilters filters)
    {
        var users = await _adminUserService.GetUsers(filters);
        return Ok(users);
    }

    // get an individual user via ID
    [HttpGet("{userId}")]
    public async Task<ActionResult<UserListItemDto>> GetUserById(string userId)
    {
        var user = await _adminUserService.GetUserById(userId);

        if (user is null)
            return NotFound("User not found.");

        return Ok(user);
    }

    // create new user
    [HttpPost]
    public async Task<ActionResult<UserListItemDto>> CreateUser(CreateUserRequest request)
    {
        var result = await _adminUserService.CreateUser(request);

        if (!result.Success)
            return StatusCode(result.StatusCode, result.Error);

        return StatusCode(result.StatusCode, result.Data);
    }

    // update user status
    [HttpPatch("{userId}/status")]
    public async Task<IActionResult> UpdateUserStatus(
    string userId,
    UpdateUserStatusRequest request)
    {
        var result = await _adminUserService.UpdateUserStatus(userId, request);

        if (!result.Success)
            return StatusCode(result.StatusCode, result.Error);

        return Ok(result.Data);
    }

    // update user attribute(s)
    [HttpPatch("{userId}")]
    public async Task<ActionResult<UserListItemDto>> UpdateUser(
    string userId,
    UpdateUserRequest request)
    {
        var result = await _adminUserService.UpdateUser(userId, request);

        if (!result.Success)
            return StatusCode(result.StatusCode, result.Error);

        return Ok(result.Data);
    }
}