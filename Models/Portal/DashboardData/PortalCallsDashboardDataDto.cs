namespace mysystem_bff.Models.Portal.DashboardData;

public class PortalCallsDashboardDataDto
{
    public string CustomerNo { get; set; } = "";
    public string SiteId { get; set; } = "";

    public int OpenCalls { get; set; }
    public int CompletedCalls { get; set; }
    public int FurtherActions { get; set; }
}