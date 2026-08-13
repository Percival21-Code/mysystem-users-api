using mysystem_bff.Models;
using mysystem_bff.Models.Admin;
using mysystem_bff.Models.Portal;
using mysystem_bff.Models.Portal.DashboardData;
using mysystem_bff.Services.Interfaces;

namespace mysystem_bff.Services.Services;

public class DashboardDataService : IDashboardDataService
{
    private readonly IMiddlewareCallsService _callsService;
    private readonly IMiddlewareReferenceService _referenceService;

    public DashboardDataService(
        IMiddlewareCallsService callsService,
        IMiddlewareReferenceService referenceService)
    {
        _callsService = callsService;
        _referenceService = referenceService;
    }

    // =========================================================
    // Calls dashboard
    // =========================================================

    public async Task<ServiceResult<PortalCallsDashboardDataDto>>
        GetCallsDashboardDataAsync(
            PortalDashboardDataQuery query,
            CancellationToken ct)
    {
        var customerNo =
            query.CustomerNo?.Trim().ToUpperInvariant() ?? "";

        var siteId =
            query.SiteId?.Trim().ToUpperInvariant() ?? "";

        if (string.IsNullOrWhiteSpace(customerNo) &&
            string.IsNullOrWhiteSpace(siteId))
        {
            return ServiceResult<PortalCallsDashboardDataDto>.Fail(
                "Either Customer No or Site ID is required.",
                400);
        }

        // -----------------------------------------------------
        // Parse year
        // -----------------------------------------------------

        var dataYear = query.DataYear;

        if (dataYear < 2000 ||
            dataYear > DateTime.UtcNow.Year + 1)
        {
            return ServiceResult<PortalCallsDashboardDataDto>.Fail(
                "A valid data year is required.",
                400);
        }

        // -----------------------------------------------------
        // Resolve requested dashboard period
        // -----------------------------------------------------

        var (loggedFrom, loggedTo) =
            GetDateRange(
                query.DataMonth,
                dataYear);

        // -----------------------------------------------------
        // Retrieve every call for the selected period
        // -----------------------------------------------------

        var callsResult =
            await GetAllCalls(
                customerNo,
                siteId,
                loggedFrom,
                loggedTo,
                ct);

        if (!callsResult.Success)
        {
            return ServiceResult<PortalCallsDashboardDataDto>.Fail(
                callsResult.Error ??
                    "Unable to retrieve calls for dashboard.",
                callsResult.StatusCode);
        }

        var calls =
            callsResult.Data ?? [];

        // -----------------------------------------------------
        // Cancelled calls are not used in dashboard statistics
        // -----------------------------------------------------

        var activeCalls = calls
            .Where(call =>
                !string.Equals(
                    call.CallType,
                    "X",
                    StringComparison.OrdinalIgnoreCase))
            .ToList();

        // =====================================================
        // KPI values
        // =====================================================

        var openCalls =
            activeCalls.Count(call =>
                IsOpenStatus(call.CallStatus));

        var completedCalls =
            activeCalls.Count(call =>
                string.Equals(
                    call.CallStatus,
                    "C",
                    StringComparison.OrdinalIgnoreCase));

        var furtherActions =
            activeCalls.Count(call =>
                string.Equals(
                    call.CallStatus,
                    "F",
                    StringComparison.OrdinalIgnoreCase));

        // =====================================================
        // Call-status donut
        // =====================================================

        var statusBreakdown = activeCalls
            .Where(call =>
                !string.IsNullOrWhiteSpace(
                    call.CallStatus))
            .GroupBy(call =>
                call.CallStatus!
                    .Trim()
                    .ToUpperInvariant())
            .Select(group =>
                new DashboardBreakdownItemDto
                {
                    Code = group.Key,
                    Label =
                        GetCallStatusLabel(
                            group.Key),
                    Count = group.Count()
                })
            .OrderByDescending(item =>
                item.Count)
            .ToList();

        // =====================================================
        // Call-type donut
        //
        // Only three high-level categories are shown:
        // PPM, Projects and Reactive.
        // =====================================================

        var ppmCount =
            activeCalls.Count(call =>
                IsPpmCallType(
                    call.CallType));

        var projectCount =
            activeCalls.Count(call =>
                IsProjectCallType(
                    call.CallType));

        var reactiveCount =
            activeCalls.Count(call =>
                IsReactiveCallType(
                    call.CallType));

        var callTypeBreakdown =
            new List<DashboardBreakdownItemDto>
            {
                new()
                {
                    Code = "PPM",
                    Label = "PPM",
                    Count = ppmCount
                },

                new()
                {
                    Code = "PROJECTS",
                    Label = "Projects",
                    Count = projectCount
                },

                new()
                {
                    Code = "REACTIVE",
                    Label = "Reactive",
                    Count = reactiveCount
                }
            }
            .Where(item => item.Count > 0)
            .ToList();

        // =====================================================
        // System-type donut
        // =====================================================

        var systemTypeBreakdown =
            await BuildSystemTypeBreakdown(
                activeCalls,
                ct);

        if (!systemTypeBreakdown.Success)
        {
            return ServiceResult<PortalCallsDashboardDataDto>.Fail(
                systemTypeBreakdown.Error ??
                    "Unable to build system type dashboard data.",
                systemTypeBreakdown.StatusCode);
        }

        // =====================================================
        // Build response
        // =====================================================

        var result =
            new PortalCallsDashboardDataDto
            {
                CustomerNo = customerNo,
                SiteId = siteId,

                OpenCalls = openCalls,
                CompletedCalls = completedCalls,
                FurtherActions = furtherActions,

                StatusBreakdown =
                    statusBreakdown,

                CallTypeBreakdown =
                    callTypeBreakdown,

                SystemTypeBreakdown =
                    systemTypeBreakdown.Data ?? []
            };

        return ServiceResult<
            PortalCallsDashboardDataDto>
            .Ok(result);
    }

    // =========================================================
    // Retrieve every page of calls
    // =========================================================

    private async Task<
        ServiceResult<List<PortalCallDto>>>
        GetAllCalls(
            string customerNo,
            string siteId,
            DateTime loggedFrom,
            DateTime loggedTo,
            CancellationToken ct)
    {
        var allCalls =
            new List<PortalCallDto>();

        var page = 1;
        var hasMore = true;

        const int maximumPages = 500;

        while (
            hasMore &&
            page <= maximumPages)
        {
            var callsQuery =
                new PortalCallsQuery
                {
                    CustomerNo = customerNo,
                    SiteId = siteId,

                    LoggedFrom = loggedFrom,
                    LoggedTo = loggedTo,

                    Page = page,
                    PageSize = 100
                };

            var result =
                await _callsService.GetCalls(
                    callsQuery,
                    ct);

            if (!result.Success)
            {
                return ServiceResult<
                    List<PortalCallDto>>
                    .Fail(
                        result.Error ??
                            "Unable to retrieve calls from middleware.",
                        result.StatusCode);
            }

            if (result.Data is null)
            {
                return ServiceResult<
                    List<PortalCallDto>>
                    .Fail(
                        "Middleware returned no call data.",
                        502);
            }

            allCalls.AddRange(
                result.Data.Items);

            hasMore =
                result.Data.HasMore;

            page++;
        }

        if (hasMore)
        {
            return ServiceResult<
                List<PortalCallDto>>
                .Fail(
                    "Dashboard call retrieval exceeded the maximum page limit.",
                    502);
        }

        return ServiceResult<
            List<PortalCallDto>>
            .Ok(allCalls);
    }

    // =========================================================
    // Build broad system-type categories
    // =========================================================

    private async Task<
    ServiceResult<List<DashboardBreakdownItemDto>>>
    BuildSystemTypeBreakdown(
        List<PortalCallDto> calls,
        CancellationToken ct)
    {
        // Get only the unique system codes that actually appear
        // in this dashboard dataset.
        var systemCodes = calls
            .Where(call =>
                !string.IsNullOrWhiteSpace(
                    call.SystemType))
            .Select(call =>
                call.SystemType!
                    .Trim()
                    .ToUpperInvariant())
            .Distinct()
            .ToList();

        var categoryCounts =
            new Dictionary<string, int>(
                StringComparer.OrdinalIgnoreCase);

        foreach (var systemCode in systemCodes)
        {
            // Resolve the raw system code through our existing
            // reference service.
            var referenceQuery =
                new PortalReferenceQuery
                {
                    Code = systemCode,
                    Page = 1,
                    PageSize = 1
                };

            var referenceResult =
                await _referenceService.GetSystemTypes(
                    referenceQuery,
                    ct);

            if (!referenceResult.Success)
            {
                return ServiceResult<
                    List<DashboardBreakdownItemDto>>
                    .Fail(
                        referenceResult.Error ??
                            $"Unable to resolve system type '{systemCode}'.",
                        referenceResult.StatusCode);
            }

            // If the reference table unexpectedly contains no record,
            // fall back to the raw code.
            var description =
                referenceResult.Data?
                    .Items
                    .FirstOrDefault()?
                    .Description
                    ?.Trim()
                ?? systemCode;

            // Convert the detailed system description into one of our
            // broad dashboard categories.
            var category =
                GetSystemCategory(description);

            // Count every call which uses this exact system code.
            var count = calls.Count(call =>
                string.Equals(
                    call.SystemType?.Trim(),
                    systemCode,
                    StringComparison.OrdinalIgnoreCase));

            if (!categoryCounts.ContainsKey(category))
            {
                categoryCounts[category] = 0;
            }

            categoryCounts[category] += count;
        }

        var result = categoryCounts
            .Select(item =>
                new DashboardBreakdownItemDto
                {
                    Code =
                        GetSystemCategoryCode(
                            item.Key),

                    Label = item.Key,

                    Count = item.Value
                })
            .OrderByDescending(item =>
                item.Count)
            .ToList();

        return ServiceResult<
            List<DashboardBreakdownItemDto>>
            .Ok(result);
    }

    // =========================================================
    // Broad system categories
    // =========================================================

    private static string GetSystemCategory(
        string description)
    {
        var cleanDescription =
            description
                .Trim()
                .ToUpperInvariant();

        if (cleanDescription.Contains("CCTV"))
        {
            return "CCTV";
        }

        if (cleanDescription.Contains("FIRE ALARM"))
        {
            return "Fire Alarm";
        }

        if (cleanDescription.Contains("EXTINGUISH"))
        {
            return "Extinguishers";
        }

        if (cleanDescription.Contains("INTRUDER"))
        {
            return "Intruder Alarm";
        }

        if (cleanDescription.Contains("IT") &&
            cleanDescription.Contains("COMMUNICATION"))
        {
            return "IT and Communications";
        }

        if (cleanDescription.Contains("ACCESS CONTROL") ||
            cleanDescription.Contains("DOOR ACCESS"))
        {
            return "Access Control";
        }

        if (cleanDescription.Contains("TELEPHON"))
        {
            return "Telephony System";
        }

        if (cleanDescription.Contains("DISABLED REFUGE") ||
            cleanDescription.Contains("REFUGE"))
        {
            return "Disabled Refuge";
        }

        // Useful additional broad categories.

        if (cleanDescription.Contains("PANIC") ||
            cleanDescription.Contains("HOLD UP"))
        {
            return "Panic Alarm";
        }

        if (cleanDescription.Contains("INTERCOM"))
        {
            return "Intercom";
        }

        if (cleanDescription.Contains("PA SYSTEM") ||
            cleanDescription.Contains("PUBLIC ADDRESS") ||
            cleanDescription.Contains("SOUND SYSTEM"))
        {
            return "PA / Sound System";
        }

        if (cleanDescription.Contains("NURSE CALL"))
        {
            return "Nurse Call";
        }

        if (cleanDescription.Contains("GATE") ||
            cleanDescription.Contains("BARRIER"))
        {
            return "Gates and Barriers";
        }

        return "Other";
    }

    private static string GetSystemCategoryCode(
        string category)
    {
        return category switch
        {
            "CCTV" =>
                "CCTV",

            "Fire Alarm" =>
                "FIRE",

            "Extinguishers" =>
                "EXT",

            "Intruder Alarm" =>
                "INTRUDER",

            "IT and Communications" =>
                "IT",

            "Access Control" =>
                "ACCESS",

            "Telephony System" =>
                "TELEPHONY",

            "Disabled Refuge" =>
                "REFUGE",

            "Panic Alarm" =>
                "PANIC",

            "Intercom" =>
                "INTERCOM",

            "PA / Sound System" =>
                "PA",

            "Nurse Call" =>
                "NURSE",

            "Gates and Barriers" =>
                "GATES",

            _ =>
                "OTHER"
        };
    }

    // =========================================================
    // Call-type categories
    // =========================================================

    private static bool IsPpmCallType(
    string? callType)
    {
        var cleanType =
            callType?
                .Trim()
                .ToUpperInvariant()
            ?? "";

        return cleanType is
            ">" or
            "P";
    }

    private static bool IsProjectCallType(
        string? callType)
    {
        var cleanType =
            callType?
                .Trim()
                .ToUpperInvariant()
            ?? "";

        return cleanType is
            "=" or
            "L" or
            "I" or
            "A" or
            "D" or
            "@" or
            "*" or
            "Š" or
            "‰" or
            "®";
    }

    private static bool IsReactiveCallType(
        string? callType)
    {
        return
            !IsPpmCallType(callType) &&
            !IsProjectCallType(callType);
    }

    // =========================================================
    // Call-status helpers
    // =========================================================

    private static bool IsOpenStatus(
        string? status)
    {
        var cleanStatus =
            status?
                .Trim()
                .ToUpperInvariant()
            ?? "";

        return cleanStatus is
            "N" or
            "A" or
            "E" or
            "F" or
            "R";
    }

    private static string GetCallStatusLabel(
        string status)
    {
        return status
            .Trim()
            .ToUpperInvariant()
            switch
        {
            "A" =>
                "Assigned to Engineer",

            "C" =>
                "Completed",

            "E" =>
                "Engineer Completed",

            "F" =>
                "Further Action Required",

            "N" =>
                "New",

            "R" =>
                "Response in Progress",

            _ =>
                "Unknown"
        };
    }

    // =========================================================
    // Date range
    // =========================================================

    private static (
        DateTime From,
        DateTime To)
        GetDateRange(
            MonthType month,
            int year)
    {
        if (month == MonthType.ALL)
        {
            var startOfYear =
                new DateTime(
                    year,
                    1,
                    1);

            return (
                startOfYear,
                startOfYear.AddYears(1)
            );
        }

        var monthNumber =
            month switch
            {
                MonthType.JAN => 1,
                MonthType.FEB => 2,
                MonthType.MAR => 3,
                MonthType.APR => 4,
                MonthType.MAY => 5,
                MonthType.JUN => 6,
                MonthType.JUL => 7,
                MonthType.AUG => 8,
                MonthType.SEP => 9,
                MonthType.OCT => 10,
                MonthType.NOV => 11,
                MonthType.DEC => 12,

                _ =>
                    throw new ArgumentOutOfRangeException(
                        nameof(month))
            };

        var startOfMonth =
            new DateTime(
                year,
                monthNumber,
                1);

        return (
            startOfMonth,
            startOfMonth.AddMonths(1)
        );
    }
}