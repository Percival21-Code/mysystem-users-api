namespace mysystem_bff.Models.Portal
{
    public class PortalSMSDto
    {
        public required string SiteId { get; set; } = "";
        public int SystemNo { get; set; }
        public string? NextMaintenanceDate { get; set; }
        public string Description { get; set; } = "";
    }
}
