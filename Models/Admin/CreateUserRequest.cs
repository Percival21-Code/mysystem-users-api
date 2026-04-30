namespace mysystem_user_api.Models.Admin;

public class CreateUserRequest
{
    public string Username { get; set; } = "";
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";

    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string? Telephone { get; set; }

    public string Role { get; set; } = "";

    public string? Position { get; set; }

    public List<string> CustomerNos { get; set; } = [];
    public List<string> SiteIds { get; set; } = [];
}