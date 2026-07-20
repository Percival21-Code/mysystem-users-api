namespace mysystem_bff.Models.Portal;

public class PortalCallDto
{
    public int CallNumber { get; set; }
    public char CallType { get; set; }
    public char CallStatus { get; set; }
    public required string? SiteID { get; set; } = null!;
    public DateTime? LoggedDate { get; set; }
    public string? LoggingOperator { get; set; }
    public string? Engineer { get; set; }
    public string? SystemType { get; set; }
    public DateTime? CompletedDate { get; set; }
    public string? CustomerReference { get; set; }
    public string? InvoiceNo { get; set; }
    public string? LoggedRemarks { get; set; }
    public string? CompletedRemarks { get; set; }
    public DateTime? PreviousMaintenanceDate { get; set; }
    public DateTime? NextMaintenanceDate { get; set; }
}