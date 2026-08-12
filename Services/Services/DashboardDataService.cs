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

    // ==================================================
    // Calls Dashboard
    // ==================================================

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

        if (query.DataYear < 2000 ||
            query.DataYear > DateTime.UtcNow.Year + 1)
        {
            return ServiceResult<PortalCallsDashboardDataDto>.Fail(
                "A valid data year is required.",
                400);
        }

        // Resolve the requested month/year into an inclusive
        // start date and exclusive end date.
        var (loggedFrom, loggedTo) = GetDateRange(
            query.DataMonth,
            query.DataYear);

        var allCallsResult = await GetAllCalls(
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

        var calls = allCallsResult.Data ?? [];

        // Cancelled calls use call type X and should not form
        // part of dashboard statistics.
        var activeCalls = calls
            .Where(call =>
                !string.Equals(
                    call.CallType,
                    "X",
                    StringComparison.OrdinalIgnoreCase))
            .ToList();

        var openCalls = activeCalls.Count(call =>
            IsOpenStatus(call.CallStatus));

        var completedCalls = activeCalls.Count(call =>
            string.Equals(
                call.CallStatus,
                "C",
                StringComparison.OrdinalIgnoreCase));

        var furtherActions = activeCalls.Count(call =>
            string.Equals(
                call.CallStatus,
                "F",
                StringComparison.OrdinalIgnoreCase));

        var result = new PortalCallsDashboardDataDto
        {
            CustomerNo = customerNo,
            SiteId = siteId,
            OpenCalls = openCalls,
            CompletedCalls = completedCalls,
            FurtherActions = furtherActions
        };

        return ServiceResult<PortalCallsDashboardDataDto>.Ok(result);
    }

    // ==================================================
    // Retrieve all calls across MMAPI pages
    // ==================================================

    private async Task<ServiceResult<List<PortalCallDto>>> GetAllCalls(
        string customerNo,
        string siteId,
        DateTime loggedFrom,
        DateTime loggedTo,
        CancellationToken ct)
    {
        var allCalls = new List<PortalCallDto>();

        var page = 1;
        var hasMore = true;

        // Safety limit in case MMAPI ever returns an incorrect
        // HasMore value.
        const int maximumPages = 500;

        while (hasMore && page <= maximumPages)
        {
            var callQuery = new PortalCallsQuery
            {
                CustomerNo = customerNo,
                SiteId = siteId,

                LoggedFrom = loggedFrom,
                LoggedTo = loggedTo,

                Page = page,
                PageSize = 100
            };

            var result = await _callsService.GetCalls(
                callQuery,
                ct);

            if (!result.Success)
            {
                return ServiceResult<List<PortalCallDto>>.Fail(
                    result.Error ??
                        "Unable to retrieve calls from middleware.",
                    result.StatusCode);
            }

            if (result.Data is null)
            {
                return ServiceResult<List<PortalCallDto>>.Fail(
                    "Middleware returned no call data.",
                    502);
            }

            allCalls.AddRange(result.Data.Items);

            hasMore = result.Data.HasMore;
            page++;
        }

        if (hasMore)
        {
            return ServiceResult<List<PortalCallDto>>.Fail(
                "Dashboard call retrieval exceeded the maximum page limit.",
                502);
        }

        return ServiceResult<List<PortalCallDto>>.Ok(allCalls);
    }

    // ==================================================
    // Resolve dashboard month/year into a date range
    // ==================================================

    private static (DateTime From, DateTime To) GetDateRange(
        MonthType month,
        int year)
    {
        if (month == MonthType.ALL)
        {
            var startOfYear = new DateTime(
                year,
                1,
                1);

            return (
                startOfYear,
                startOfYear.AddYears(1)
            );
        }

        var monthNumber = month switch
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

            _ => throw new ArgumentOutOfRangeException(
                nameof(month))
        };

        var startOfMonth = new DateTime(
            year,
            monthNumber,
            1);

        return (
            startOfMonth,
            startOfMonth.AddMonths(1)
        );
    }

    // ==================================================
    // Call status helpers
    // ==================================================

    private static bool IsOpenStatus(string? status)
    {
        var cleanStatus =
            status?.Trim().ToUpperInvariant() ?? "";

        return cleanStatus is
            "N" or // New
            "A" or // Assigned
            "E" or // Engineer completed, not closed/invoiced
            "F" or // Further action
            "R";   // Response in progress
    }
}