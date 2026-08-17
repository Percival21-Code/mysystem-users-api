namespace mysystem_bff.Models.Portal.DashboardData;

public class PortalMaintenanceDashboardItemDto
{
    public string SiteId { get; set; } = "";

    public int SystemNo { get; set; }

    public DateTime NextMaintenanceDate { get; set; }

    public string Description { get; set; } = "";

    public string StatusCode { get; set; } = "";

    public string StatusLabel { get; set; } = "";
}