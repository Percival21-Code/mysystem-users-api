namespace mysystem_user_api.Models.Middleware;

public class MiddlewareSitesResponse
{
    public List<MiddlewareSite> Items { get; set; } = [];
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
    public bool HasMore { get; set; }
}