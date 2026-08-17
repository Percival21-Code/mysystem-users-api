namespace mysystem_bff.Models.Portal.DashboardData;

public class PortalMaintenanceDashboardDataDto
{
    public string CustomerNo { get; set; } = "";
    public string SiteId { get; set; } = "";

    public int UpToDate { get; set; }
    public int DueSoon { get; set; }
    public int Overdue { get; set; }

    public List<DashboardBreakdownItemDto> MaintenanceStatusBreakdown { get; set; } = [];
    public List<DashboardBreakdownItemDto> DueSoonBreakdown { get; set; } = [];
}