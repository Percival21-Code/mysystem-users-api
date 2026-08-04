namespace mysystem_bff.Models.Portal
{
    public class PortalCallActionDto
    {
        public int? CallNumber { get; set; }
        public int? CallActionNumber { get; set; }
        public string? Remarks { get; set; } // office given remarks
        public string? AppointmentDate { get; set; }
        public string? AppointmentFromTime { get; set; }
        public string? StartedDate { get; set; } // call started
        public string? StartedTime { get; set; }
        public string? FinishedDate { get; set; } // call finished
        public string? FinishedTime { get; set; }
        public int? HoursOnSite { get; set; } // time on site
        public int? MinutesOnSite { get; set; }
        public string? Engineer { get; set; } // engineer ref code
        public string? ActionTaken { get; set; } // engineer action
        public string? SignatureName { get; set; } // customer signature name
        public string? OnCallEngineersName { get; set; } // operating engineer name


        public string? OnRouteDate { get; set; } // engineer marked self as on route
        public string? OnRouteTime { get; set; }
        public string? OnSiteDate { get; set; } // engineer marked self as on site
        public string? OnSiteTime { get; set; }

        // SLA details
        public string? SLADeadlineDate { get; set; }
        public string? SLADeadlineTime { get; set; }
        public string? SLAStartDate { get; set; }
        public string? SLAStartTime { get; set; }

        // overtime details
        public string? OvertimeType { get; set; }
        public string? OvertimeStartDate { get; set; }
        public string? OvertimeStartTime { get; set; }
        public string? OvertimeFinishDate { get; set; }
        public string? OvertimeFinishTime { get; set; }
        public string? RemoteFix_YN { get; set; } // remote fix

        // key site info 
        public string? PropertyReferenceNo { get; set; }
        public string? Name { get; set; }
        public string? CallStatus { get; set; }
        public string? CustomerReference { get; set; }
        public string? SiteName { get; set; }
    }
}
