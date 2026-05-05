using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using mysystem_user_api.Models.Admin;
using mysystem_user_api.Services.Interfaces;

namespace mysystem_user_api.Controllers;

[ApiController]
[Route("api/admin/users")]
[Authorize(Policy = "AdministratorOnly")]
public class AdminUsersController : ControllerBase
{
    private readonly IAdminUserReadService _readService;
    private readonly IAdminUserCreateService _createService;
    private readonly IAdminUserUpdateService _updateService;
    private readonly IAdminUserStatusService _statusService;

    public AdminUsersController(
        IAdminUserReadService readService,
        IAdminUserCreateService createService,
        IAdminUserUpdateService updateService,
        IAdminUserStatusService statusService)
    {
        _readService = readService;
        _createService = createService;
        _updateService = updateService;
        _statusService = statusService;
    }

    [HttpGet]
    public async Task<ActionResult<List<UserListItemDto>>> GetUsers([FromQuery] UserFilters filters)
    {
        var users = await _readService.GetUsers(filters);
        return Ok(users);
    }

    [HttpGet("{userId}")]
    public async Task<ActionResult<UserListItemDto>> GetUserById(string userId)
    {
        var user = await _readService.GetUserById(userId);

        if (user is null)
            return NotFound("User not found.");

        return Ok(user);
    }

    [HttpPost]
    public async Task<ActionResult<UserListItemDto>> CreateUser(CreateUserRequest request)
    {
        var result = await _createService.CreateUser(request);

        if (!result.Success)
            return StatusCode(result.StatusCode, result.Error);

        return StatusCode(result.StatusCode, result.Data);
    }

    [HttpPatch("{userId}")]
    public async Task<ActionResult<UserListItemDto>> UpdateUser(
        string userId,
        UpdateUserRequest request)
    {
        var result = await _updateService.UpdateUser(userId, request);

        if (!result.Success)
            return StatusCode(result.StatusCode, result.Error);

        return Ok(result.Data);
    }

    [HttpPatch("{userId}/status")]
    public async Task<IActionResult> UpdateUserStatus(
        string userId,
        UpdateUserStatusRequest request)
    {
        var result = await _statusService.UpdateUserStatus(userId, request);

        if (!result.Success)
            return StatusCode(result.StatusCode, result.Error);

        return Ok(result.Data);
    }
}