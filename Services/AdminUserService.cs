using Dapper;
using MySqlConnector;
using mysystem_user_api.Models.Admin;

namespace mysystem_user_api.Services;

public class AdminUserService : IAdminUserService
{
    private readonly MySqlConnection _db;

    public AdminUserService(MySqlConnection db)
    {
        _db = db;
    }

    public async Task<List<UserListItemDto>> GetUsers(UserFilters filters)
    {
        var sql = """
            SELECT DISTINCT
                CAST(u.user_id AS CHAR) AS UserId,
                u.username AS Username,
                u.email AS Email,
                u.first_name AS FirstName,
                u.last_name AS LastName,
                u.telephone AS Telephone,
                u.is_active AS IsActive,
                u.created_at AS CreatedAt,
                sp.position AS Position
            FROM users u
            LEFT JOIN staff_profiles sp ON sp.user_id = u.user_id
            LEFT JOIN user_roles ur ON ur.user_id = u.user_id
            LEFT JOIN roles r ON r.role_id = ur.role_id
            LEFT JOIN user_customer_access uca ON uca.user_id = u.user_id
            LEFT JOIN user_site_access usa ON usa.user_id = u.user_id
            WHERE 1 = 1
        """;

        var parameters = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(filters.UserId))
        {
            sql += " AND CAST(u.user_id AS CHAR) = @UserId";
            parameters.Add("UserId", filters.UserId);
        }

        if (!string.IsNullOrWhiteSpace(filters.Role))
        {
            sql += " AND r.role_name = @Role";
            parameters.Add("Role", filters.Role);
        }

        if (!string.IsNullOrWhiteSpace(filters.CustomerNo))
        {
            sql += " AND uca.customer_no = @CustomerNo";
            parameters.Add("CustomerNo", filters.CustomerNo);
        }

        if (!string.IsNullOrWhiteSpace(filters.SiteId))
        {
            sql += " AND usa.site_id = @SiteId";
            parameters.Add("SiteId", filters.SiteId);
        }

        if (filters.IsActive.HasValue)
        {
            sql += " AND u.is_active = @IsActive";
            parameters.Add("IsActive", filters.IsActive.Value);
        }

        sql += " ORDER BY u.created_at DESC;";

        var users = (await _db.QueryAsync<UserListItemDto>(sql, parameters)).ToList();

        foreach (var user in users)
        {
            await PopulateUserExtras(user);
        }

        return users;
    }

    public async Task<UserListItemDto?> GetUserById(string userId)
    {
        var user = await _db.QuerySingleOrDefaultAsync<UserListItemDto>(
            """
            SELECT
                CAST(u.user_id AS CHAR) AS UserId,
                u.username AS Username,
                u.email AS Email,
                u.first_name AS FirstName,
                u.last_name AS LastName,
                u.telephone AS Telephone,
                u.is_active AS IsActive,
                u.created_at AS CreatedAt,
                sp.position AS Position
            FROM users u
            LEFT JOIN staff_profiles sp ON sp.user_id = u.user_id
            WHERE CAST(u.user_id AS CHAR) = @UserId
            LIMIT 1;
            """,
            new { UserId = userId }
        );

        if (user is null)
            return null;

        await PopulateUserExtras(user);

        return user;
    }

    public async Task<ServiceResult<UserListItemDto>> CreateUser(CreateUserRequest request)
    {
        var validationError = ValidateCreateUserRequest(request);

        if (validationError is not null)
            return ServiceResult<UserListItemDto>.Fail(validationError, 400);

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

        var existingUser = await _db.QuerySingleOrDefaultAsync<string>(
            """
            SELECT CAST(user_id AS CHAR)
            FROM users
            WHERE username = @Username OR email = @Email
            LIMIT 1;
            """,
            new
            {
                request.Username,
                request.Email
            }
        );

        if (existingUser is not null)
            return ServiceResult<UserListItemDto>.Fail(
                "A user with this username or email already exists.",
                409
            );

        var userId = Guid.NewGuid().ToString();
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, workFactor: 12);

        await _db.OpenAsync();

        await using var transaction = await _db.BeginTransactionAsync();

        try
        {
            await _db.ExecuteAsync(
                """
                INSERT INTO users (
                    user_id,
                    username,
                    email,
                    password_hash,
                    first_name,
                    last_name,
                    telephone,
                    is_active
                )
                VALUES (
                    @UserId,
                    @Username,
                    @Email,
                    @PasswordHash,
                    @FirstName,
                    @LastName,
                    @Telephone,
                    TRUE
                );
                """,
                new
                {
                    UserId = userId,
                    request.Username,
                    request.Email,
                    PasswordHash = passwordHash,
                    request.FirstName,
                    request.LastName,
                    request.Telephone
                },
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

            foreach (var customerNo in request.CustomerNos.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct())
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

            foreach (var siteId in request.SiteIds.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct())
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

        var createdUser = await GetUserById(userId);

        if (createdUser is null)
            return ServiceResult<UserListItemDto>.Fail("User was created but could not be loaded.", 500);

        return ServiceResult<UserListItemDto>.Created(createdUser);
    }

    private static string? ValidateCreateUserRequest(CreateUserRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username))
            return "Username is required.";

        if (string.IsNullOrWhiteSpace(request.Email))
            return "Email is required.";

        if (string.IsNullOrWhiteSpace(request.Password))
            return "Password is required.";

        if (string.IsNullOrWhiteSpace(request.FirstName))
            return "First name is required.";

        if (string.IsNullOrWhiteSpace(request.LastName))
            return "Last name is required.";

        if (string.IsNullOrWhiteSpace(request.Role))
            return "Role is required.";

        return null;
    }

    private async Task PopulateUserExtras(UserListItemDto user)
    {
        user.Roles = (await _db.QueryAsync<string>(
            """
            SELECT r.role_name
            FROM roles r
            INNER JOIN user_roles ur ON ur.role_id = r.role_id
            WHERE CAST(ur.user_id AS CHAR) = @UserId
            ORDER BY r.role_name;
            """,
            new { user.UserId }
        )).ToList();

        user.CustomerNos = (await _db.QueryAsync<string>(
            """
            SELECT customer_no
            FROM user_customer_access
            WHERE CAST(user_id AS CHAR) = @UserId
            ORDER BY customer_no;
            """,
            new { user.UserId }
        )).ToList();

        user.SiteIds = (await _db.QueryAsync<string>(
            """
            SELECT site_id
            FROM user_site_access
            WHERE CAST(user_id AS CHAR) = @UserId
            ORDER BY site_id;
            """,
            new { user.UserId }
        )).ToList();
    }
}