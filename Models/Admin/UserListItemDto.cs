namespace mysystem_bff.Models.Admin;

public class UserListItemDto
{
    public string UserId { get; set; } = "";
    public string Username { get; set; } = "";
    public string Email { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string? Telephone { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }

    public string? Position { get; set; }

    public List<string> Roles { get; set; } = [];
    public List<string> CustomerNos { get; set; } = [];
    public List<string> SiteIds { get; set; } = [];
}