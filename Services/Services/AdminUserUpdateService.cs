using Dapper;
using MySqlConnector;
using mysystem_user_api.Models.Admin;
using mysystem_user_api.Services.Interfaces;

namespace mysystem_user_api.Services.Services;

public class AdminUserUpdateService : IAdminUserUpdateService
{
    private readonly MySqlConnection _db;
    private readonly IAdminUserReadService _readService;

    public AdminUserUpdateService(
        MySqlConnection db,
        IAdminUserReadService readService)
    {
        _db = db;
        _readService = readService;
    }

    public async Task<ServiceResult<UserListItemDto>> UpdateUser(
        string userId,
        UpdateUserRequest request)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return ServiceResult<UserListItemDto>.Fail("User ID is required.", 400);

        var validationError = ValidateUpdateUserRequest(request);

        if (validationError is not null)
            return ServiceResult<UserListItemDto>.Fail(validationError, 400);

        var userExists = await _db.QuerySingleOrDefaultAsync<string>(
            """
            SELECT CAST(user_id AS CHAR)
            FROM users
            WHERE CAST(user_id AS CHAR) = @UserId
            LIMIT 1;
            """,
            new { UserId = userId }
        );

        if (userExists is null)
            return ServiceResult<UserListItemDto>.Fail("User not found.", 404);

        var duplicateUser = await _db.QuerySingleOrDefaultAsync<string>(
            """
            SELECT CAST(user_id AS CHAR)
            FROM users
            WHERE 
                (username = @Username OR email = @Email)
                AND CAST(user_id AS CHAR) <> @UserId
            LIMIT 1;
            """,
            new
            {
                UserId = userId,
                request.Username,
                request.Email
            }
        );

        if (duplicateUser is not null)
        {
            return ServiceResult<UserListItemDto>.Fail(
                "Another user already has this username or email.",
                409
            );
        }

        var roleId = await _db.QuerySingleOrDefaultAsync<int?>(
            """
            SELECT role_id
            FROM roles
            WHERE role_name = @Role
            LIMIT 1;
            """,
            new { request.Role }
        );

        if (roleId is null)
            return ServiceResult<UserListItemDto>.Fail("Invalid role.", 400);

        await _db.OpenAsync();
        await using var transaction = await _db.BeginTransactionAsync();

        try
        {
            await _db.ExecuteAsync(
                """
                UPDATE users
                SET
                    username = @Username,
                    email = @Email,
                    first_name = @FirstName,
                    last_name = @LastName,
                    telephone = @Telephone,
                    updated_at = CURRENT_TIMESTAMP
                WHERE CAST(user_id AS CHAR) = @UserId;
                """,
                new
                {
                    UserId = userId,
                    request.Username,
                    request.Email,
                    request.FirstName,
                    request.LastName,
                    request.Telephone
                },
                transaction
            );

            await _db.ExecuteAsync(
                """
                DELETE FROM user_roles
                WHERE CAST(user_id AS CHAR) = @UserId;
                """,
                new { UserId = userId },
                transaction
            );

            await _db.ExecuteAsync(
                """
                INSERT INTO user_roles (
                    user_id,
                    role_id
                )
                VALUES (
                    @UserId,
                    @RoleId
                );
                """,
                new
                {
                    UserId = userId,
                    RoleId = roleId.Value
                },
                transaction
            );

            await _db.ExecuteAsync(
                """
                DELETE FROM staff_profiles
                WHERE CAST(user_id AS CHAR) = @UserId;
                """,
                new { UserId = userId },
                transaction
            );

            if (!string.IsNullOrWhiteSpace(request.Position))
            {
                await _db.ExecuteAsync(
                    """
                    INSERT INTO staff_profiles (
                        user_id,
                        position
                    )
                    VALUES (
                        @UserId,
                        @Position
                    );
                    """,
                    new
                    {
                        UserId = userId,
                        request.Position
                    },
                    transaction
                );
            }

            await _db.ExecuteAsync(
                """
                DELETE FROM user_customer_access
                WHERE CAST(user_id AS CHAR) = @UserId;
                """,
                new { UserId = userId },
                transaction
            );

            foreach (var customerNo in request.CustomerNos
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct())
            {
                await _db.ExecuteAsync(
                    """
                    INSERT INTO user_customer_access (
                        user_id,
                        customer_no
                    )
                    VALUES (
                        @UserId,
                        @CustomerNo
                    );
                    """,
                    new
                    {
                        UserId = userId,
                        CustomerNo = customerNo
                    },
                    transaction
                );
            }

            await _db.ExecuteAsync(
                """
                DELETE FROM user_site_access
                WHERE CAST(user_id AS CHAR) = @UserId;
                """,
                new { UserId = userId },
                transaction
            );

            foreach (var siteId in request.SiteIds
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct())
            {
                await _db.ExecuteAsync(
                    """
                    INSERT INTO user_site_access (
                        user_id,
                        site_id
                    )
                    VALUES (
                        @UserId,
                        @SiteId
                    );
                    """,
                    new
                    {
                        UserId = userId,
                        SiteId = siteId
                    },
                    transaction
                );
            }

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }

        var updatedUser = await _readService.GetUserById(userId);

        if (updatedUser is null)
            return ServiceResult<UserListItemDto>.Fail("User was updated but could not be loaded.", 500);

        return ServiceResult<UserListItemDto>.Ok(updatedUser);
    }

    private static string? ValidateUpdateUserRequest(UpdateUserRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username))
            return "Username is required.";

        if (string.IsNullOrWhiteSpace(request.Email))
            return "Email is required.";

        if (string.IsNullOrWhiteSpace(request.FirstName))
            return "First name is required.";

        if (string.IsNullOrWhiteSpace(request.LastName))
            return "Last name is required.";

        if (string.IsNullOrWhiteSpace(request.Role))
            return "Role is required.";

        return null;
    }
}