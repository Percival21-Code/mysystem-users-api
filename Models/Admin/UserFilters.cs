namespace mysystem_user_api.Models.Admin;

public class UserFilters
{
    public string? UserId { get; set; }
    public string? Role { get; set; }
    public string? CustomerNo { get; set; }
    public string? SiteId { get; set; }
    public bool? IsActive { get; set; }
}