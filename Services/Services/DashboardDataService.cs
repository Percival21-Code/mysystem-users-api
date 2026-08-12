using mysystem_bff.Models;
using mysystem_bff.Models.Admin;
using mysystem_bff.Models.Portal;
using mysystem_bff.Models.Portal.DashboardData;
using mysystem_bff.Services.Interfaces;

namespace mysystem_bff.Services.Services;

public class DashboardDataService : IDashboardDataService
{
    private readonly IMiddlewareCallsService _callsService;

    public DashboardDataService(
        IMiddlewareCallsService callsService)
    {
        _callsService = callsService;
    }

    // =========================================================
    // Calls Dashboard
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

        // At least one scope is required.
        if (string.IsNullOrWhiteSpace(customerNo) &&
            string.IsNullOrWhiteSpace(siteId))
        {
            return ServiceResult<PortalCallsDashboardDataDto>.Fail(
                "Either Customer No or Site ID is required.",
                400);
        }

        // Validate dashboard year.
        if (query.DataYear < 2000 ||
            query.DataYear > DateTime.UtcNow.Year + 1)
        {
            return ServiceResult<PortalCallsDashboardDataDto>.Fail(
                "A valid data year is required.",
                400);
        }

        // Convert dashboard month/year selection into
        // the date range expected by the Calls service.
        var (loggedFrom, loggedTo) =
            GetDateRange(
                query.DataMonth,
                query.DataYear);

        // Retrieve every call for the requested period.
        var allCallsResult =
            await GetAllCalls(
                customerNo,
                siteId,
                loggedFrom,
                loggedTo,
                ct);

        if (!allCallsResult.Success)
        {
            return ServiceResult<PortalCallsDashboardDataDto>.Fail(
                allCallsResult.Error ??
                    "Unable to retrieve calls for dashboard.",
                allCallsResult.StatusCode);
        }

        var calls =
            allCallsResult.Data ?? [];

        // =====================================================
        // Remove cancelled calls
        // =====================================================

        var activeCalls = calls
            .Where(call =>
                !string.Equals(
                    call.CallType,
                    "X",
                    StringComparison.OrdinalIgnoreCase))
            .ToList();

        // =====================================================
        // KPI data
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
        // Call status donut data
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
        // Call type donut data
        // =====================================================

        var callTypeBreakdown = activeCalls
            .Where(call =>
                !string.IsNullOrWhiteSpace(
                    call.CallType))
            .GroupBy(call =>
                call.CallType!
                    .Trim()
                    .ToUpperInvariant())
            .Select(group =>
                new DashboardBreakdownItemDto
                {
                    Code = group.Key,

                    // For now use the raw code.
                    // We can resolve this through reference
                    // data later if required.
                    Label = group.Key,

                    Count = group.Count()
                })
            .OrderByDescending(item =>
                item.Count)
            .ToList();

        // =====================================================
        // System type donut data
        // =====================================================

        var systemTypeBreakdown = activeCalls
            .Where(call =>
                !string.IsNullOrWhiteSpace(
                    call.SystemType))
            .GroupBy(call =>
                call.SystemType!
                    .Trim()
                    .ToUpperInvariant())
            .Select(group =>
                new DashboardBreakdownItemDto
                {
                    Code = group.Key,

                    // Raw system code for now.
                    // e.g. A1, F1AA etc.
                    Label = group.Key,

                    Count = group.Count()
                })
            .OrderByDescending(item =>
                item.Count)
            .ToList();

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
                    systemTypeBreakdown
            };

        return ServiceResult<
            PortalCallsDashboardDataDto>
            .Ok(result);
    }

    // =========================================================
    // Retrieve every Calls page
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

        // Prevent an accidental endless loop if MMAPI
        // ever returns broken paging information.
        const int maximumPages = 500;

        while (
            hasMore &&
            page <= maximumPages)
        {
            var callQuery =
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
                    callQuery,
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
    // Dashboard date range
    // =========================================================

    private static (
        DateTime From,
        DateTime To)
        GetDateRange(
            MonthType month,
            int year)
    {
        // Whole year.
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

    // =========================================================
    // Open-call status definition
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
            "N" or // New
            "A" or // Assigned to Engineer
            "E" or // Engineer completed, not invoiced
            "F" or // Further action required
            "R";   // Response in progress
    }

    // =========================================================
    // Call-status descriptions
    // =========================================================

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
}