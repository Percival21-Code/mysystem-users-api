using mysystem_bff.Models.Admin;
using mysystem_bff.Models.Portal;
using mysystem_bff.Models.Portal.DashboardData;
using mysystem_bff.Services.Dashboard;
using mysystem_bff.Services.Interfaces;

namespace mysystem_bff.Services.Services;

public class MaintenanceDashboardService
    : IMaintenanceDashboardService
{
    private readonly IMiddlewareSitesService _sitesService;
    private readonly IMiddlewareSmsService _smsService;

    public MaintenanceDashboardService(
        IMiddlewareSitesService sitesService,
        IMiddlewareSmsService smsService)
    {
        _sitesService = sitesService;
        _smsService = smsService;
    }

    // =========================================================
    // MAINTENANCE DASHBOARD SUMMARY
    // =========================================================

    public async Task<ServiceResult<PortalMaintenanceDashboardDataDto>>
        GetDashboardDataAsync(
            PortalDashboardDataQuery query,
            CancellationToken ct = default)
    {
        var customerNo =
            query.CustomerNo?.Trim().ToUpperInvariant() ?? "";

        var siteId =
            query.SiteId?.Trim().ToUpperInvariant() ?? "";

        if (string.IsNullOrWhiteSpace(customerNo) &&
            string.IsNullOrWhiteSpace(siteId))
        {
            return ServiceResult<PortalMaintenanceDashboardDataDto>.Fail(
                "Either Customer No or Site ID is required.",
                400);
        }

        if (!DashboardDateHelper.IsValidYear(
            query.DataYear))
        {
            return ServiceResult<PortalMaintenanceDashboardDataDto>.Fail(
                "A valid data year is required.",
                400);
        }

        /*
         * Unlike Calls Dashboard, this range applies to
         * NextMaintenanceDate rather than LoggedDate.
         */
        var (maintenanceFrom, maintenanceTo) =
            DashboardDateHelper.GetDateRange(
                query.DataMonth,
                query.DataYear);

        var schedulesResult =
            await GetMaintenanceSchedulesForDashboard(
                customerNo,
                siteId,
                maintenanceFrom,
                maintenanceTo,
                ct);

        if (!schedulesResult.Success)
        {
            return ServiceResult<PortalMaintenanceDashboardDataDto>.Fail(
                schedulesResult.Error ??
                    "Unable to retrieve maintenance schedules for dashboard.",
                schedulesResult.StatusCode);
        }

        var today =
            DateTime.UtcNow.Date;

        var items =
            BuildMaintenanceDashboardItems(
                schedulesResult.Data ?? [],
                today);

        // =====================================================
        // KPI DATA
        // =====================================================

        var upToDate =
            items.Count(item =>
                item.StatusCode == "UP_TO_DATE");

        var dueSoon =
            items.Count(item =>
                item.StatusCode == "DUE_SOON");

        var overdue =
            items.Count(item =>
                item.StatusCode == "OVERDUE");

        // =====================================================
        // STATUS BREAKDOWN
        // =====================================================

        var maintenanceStatusBreakdown =
            new List<DashboardBreakdownItemDto>
            {
                new()
                {
                    Code = "UP_TO_DATE",
                    Label = "Up to date",
                    Count = upToDate
                },

                new()
                {
                    Code = "DUE_SOON",
                    Label = "Due soon",
                    Count = dueSoon
                },

                new()
                {
                    Code = "OVERDUE",
                    Label = "Overdue",
                    Count = overdue
                }
            };

        // =====================================================
        // DUE SOON BREAKDOWN
        // =====================================================

        var dueSoonBreakdown =
            new List<DashboardBreakdownItemDto>
            {
                new()
                {
                    Code = "WITHIN_7_DAYS",
                    Label = "Within 7 days",

                    Count = items.Count(item =>
                        MatchesMaintenanceFilter(
                            item,
                            DashboardMaintenanceFilterType.WITHIN_7_DAYS,
                            today))
                },

                new()
                {
                    Code = "DAYS_8_TO_14",
                    Label = "8-14 days",

                    Count = items.Count(item =>
                        MatchesMaintenanceFilter(
                            item,
                            DashboardMaintenanceFilterType.DAYS_8_TO_14,
                            today))
                },

                new()
                {
                    Code = "DAYS_15_TO_30",
                    Label = "15-30 days",

                    Count = items.Count(item =>
                        MatchesMaintenanceFilter(
                            item,
                            DashboardMaintenanceFilterType.DAYS_15_TO_30,
                            today))
                },

                new()
                {
                    Code = "DAYS_31_TO_90",
                    Label = "31-90 days",

                    Count = items.Count(item =>
                        MatchesMaintenanceFilter(
                            item,
                            DashboardMaintenanceFilterType.DAYS_31_TO_90,
                            today))
                }
            };

        // =====================================================
        // RESULT
        // =====================================================

        var result =
            new PortalMaintenanceDashboardDataDto
            {
                CustomerNo = customerNo,
                SiteId = siteId,

                UpToDate = upToDate,
                DueSoon = dueSoon,
                Overdue = overdue,

                MaintenanceStatusBreakdown =
                    maintenanceStatusBreakdown,

                DueSoonBreakdown =
                    dueSoonBreakdown
            };

        return ServiceResult<PortalMaintenanceDashboardDataDto>.Ok(
            result);
    }

    // =========================================================
    // MAINTENANCE DASHBOARD SUPPORTING RECORDS
    // =========================================================

    public async Task<ServiceResult<PortalDashboardMaintenanceItemsResponse>>
        GetDashboardItemsAsync(
            PortalDashboardMaintenanceItemsQuery query,
            CancellationToken ct = default)
    {
        var customerNo =
            query.CustomerNo?.Trim().ToUpperInvariant() ?? "";

        var siteId =
            query.SiteId?.Trim().ToUpperInvariant() ?? "";

        if (string.IsNullOrWhiteSpace(customerNo) &&
            string.IsNullOrWhiteSpace(siteId))
        {
            return ServiceResult<PortalDashboardMaintenanceItemsResponse>.Fail(
                "Either Customer No or Site ID is required.",
                400);
        }

        if (!DashboardDateHelper.IsValidYear(
            query.DataYear))
        {
            return ServiceResult<PortalDashboardMaintenanceItemsResponse>.Fail(
                "A valid data year is required.",
                400);
        }

        var page =
            query.Page > 0
                ? query.Page
                : 1;

        // Hard dashboard limit.
        var pageSize =
            Math.Clamp(
                query.PageSize,
                1,
                30);

        var (maintenanceFrom, maintenanceTo) =
            DashboardDateHelper.GetDateRange(
                query.DataMonth,
                query.DataYear);

        var schedulesResult =
            await GetMaintenanceSchedulesForDashboard(
                customerNo,
                siteId,
                maintenanceFrom,
                maintenanceTo,
                ct);

        if (!schedulesResult.Success)
        {
            return ServiceResult<PortalDashboardMaintenanceItemsResponse>.Fail(
                schedulesResult.Error ??
                    "Unable to retrieve maintenance schedules for dashboard.",
                schedulesResult.StatusCode);
        }

        var today =
            DateTime.UtcNow.Date;

        var filteredItems =
            BuildMaintenanceDashboardItems(
                    schedulesResult.Data ?? [],
                    today)
                .Where(item =>
                    MatchesMaintenanceFilter(
                        item,
                        query.FilterType,
                        today))
                .OrderBy(item =>
                    item.NextMaintenanceDate)
                .ThenBy(item =>
                    item.SiteId,
                    StringComparer.OrdinalIgnoreCase)
                .ThenBy(item =>
                    item.SystemNo)
                .ToList();

        var total =
            filteredItems.Count;

        var items =
            filteredItems
                .Skip(
                    (page - 1) *
                    pageSize)
                .Take(pageSize)
                .ToList();

        var result =
            new PortalDashboardMaintenanceItemsResponse
            {
                Items = items,

                Page = page,
                PageSize = pageSize,
                Total = total,

                HasMore =
                    page * pageSize < total
            };

        return ServiceResult<PortalDashboardMaintenanceItemsResponse>.Ok(
            result);
    }

    // =========================================================
    // RETRIEVE MAINTENANCE SCHEDULES
    // =========================================================

    private async Task<ServiceResult<List<PortalSMSDto>>>
        GetMaintenanceSchedulesForDashboard(
            string customerNo,
            string siteId,
            DateTime maintenanceFrom,
            DateTime maintenanceTo,
            CancellationToken ct)
    {
        var siteIdsResult =
            await GetMaintenanceDashboardSiteIds(
                customerNo,
                siteId,
                ct);

        if (!siteIdsResult.Success)
        {
            return ServiceResult<List<PortalSMSDto>>.Fail(
                siteIdsResult.Error ??
                    "Unable to retrieve sites for maintenance dashboard.",
                siteIdsResult.StatusCode);
        }

        var siteIds =
            siteIdsResult.Data ?? [];

        if (siteIds.Count == 0)
        {
            return ServiceResult<List<PortalSMSDto>>.Ok([]);
        }

        /*
         * Customer-wide:
         *
         * We already know CustomerNo is valid for the portal user,
         * so pass it through. MMAPI then avoids resolving CustomerNo
         * from each individual Site ID.
         *
         * Specific-site:
         *
         * Leave CustomerNo blank. MMAPI resolves the site's actual
         * owning customer from SiteId rather than trusting a CustomerNo
         * that may have been separately supplied in the request.
         */
        var middlewareCustomerNo =
            string.IsNullOrWhiteSpace(siteId)
                ? customerNo
                : "";

        var schedulesResult =
            await _smsService.GetMaintenanceSchedulesForSites(
                middlewareCustomerNo,
                siteIds,
                maintenanceFrom,
                maintenanceTo,
                ct);

        if (!schedulesResult.Success)
        {
            return ServiceResult<List<PortalSMSDto>>.Fail(
                schedulesResult.Error ??
                    "Unable to retrieve maintenance schedules from middleware.",
                schedulesResult.StatusCode);
        }

        /*
         * MMAPI receives the same date filters, but reapply them here.
         *
         * The BFF owns the portal dashboard contract, so the dashboard
         * should remain correct even if MMAPI's filtering changes later.
         */
        var filteredSchedules =
            (schedulesResult.Data ?? [])
                .Where(schedule =>
                {
                    if (!TryGetMaintenanceDate(
                        schedule.NextMaintenanceDate,
                        out var maintenanceDate))
                    {
                        return false;
                    }

                    return
                        maintenanceDate.Date >= maintenanceFrom.Date &&
                        maintenanceDate.Date <= maintenanceTo.Date;
                })
                .ToList();

        return ServiceResult<List<PortalSMSDto>>.Ok(
            filteredSchedules);
    }

    // =========================================================
    // RETRIEVE CUSTOMER SITE IDS
    // =========================================================

    private async Task<ServiceResult<List<string>>>
        GetMaintenanceDashboardSiteIds(
            string customerNo,
            string siteId,
            CancellationToken ct)
    {
        /*
         * Specific-site dashboard does not need to retrieve the
         * customer's entire site collection.
         */
        if (!string.IsNullOrWhiteSpace(siteId))
        {
            return ServiceResult<List<string>>.Ok(
                [siteId]);
        }

        if (string.IsNullOrWhiteSpace(customerNo))
        {
            return ServiceResult<List<string>>.Fail(
                "Customer No is required when a Site ID is not supplied.",
                400);
        }

        var siteIds =
            new List<string>();

        var page = 1;
        var hasMore = true;

        const int maximumPages = 500;

        while (
            hasMore &&
            page <= maximumPages)
        {
            var sitesResult =
                await _sitesService.GetSites(
                    new PortalSitesQuery
                    {
                        CustomerNo = customerNo,
                        Page = page,
                        PageSize = 100
                    },
                    ct);

            if (!sitesResult.Success)
            {
                return ServiceResult<List<string>>.Fail(
                    sitesResult.Error ??
                        "Unable to retrieve customer sites from middleware.",
                    sitesResult.StatusCode);
            }

            if (sitesResult.Data is null)
            {
                return ServiceResult<List<string>>.Fail(
                    "Middleware returned no site data.",
                    502);
            }

            siteIds.AddRange(
                sitesResult.Data.Items
                    .Select(site =>
                        site.SiteId?
                            .Trim()
                            .ToUpperInvariant()
                        ?? "")
                    .Where(value =>
                        !string.IsNullOrWhiteSpace(value)));

            hasMore =
                sitesResult.Data.HasMore;

            page++;
        }

        if (hasMore)
        {
            return ServiceResult<List<string>>.Fail(
                "Maintenance dashboard site retrieval exceeded the maximum page limit.",
                502);
        }

        return ServiceResult<List<string>>.Ok(
            siteIds
                .Distinct(
                    StringComparer.OrdinalIgnoreCase)
                .ToList());
    }

    // =========================================================
    // MAINTENANCE ITEM CREATION
    // =========================================================

    private static List<PortalMaintenanceDashboardItemDto>
        BuildMaintenanceDashboardItems(
            IEnumerable<PortalSMSDto> schedules,
            DateTime today)
    {
        var items =
            new List<PortalMaintenanceDashboardItemDto>();

        foreach (var schedule in schedules)
        {
            if (!TryGetMaintenanceDate(
                schedule.NextMaintenanceDate,
                out var nextMaintenanceDate))
            {
                /*
                 * A schedule without a valid NextMaintenanceDate cannot
                 * be meaningfully placed into this dashboard.
                 */
                continue;
            }

            var statusCode =
                GetMaintenanceStatusCode(
                    nextMaintenanceDate,
                    today);

            items.Add(
                new PortalMaintenanceDashboardItemDto
                {
                    SiteId =
                        schedule.SiteId?
                            .Trim()
                            .ToUpperInvariant()
                        ?? "",

                    SystemNo =
                        schedule.SystemNo,

                    NextMaintenanceDate =
                        nextMaintenanceDate.Date,

                    Description =
                        schedule.Description ?? "",

                    StatusCode =
                        statusCode,

                    StatusLabel =
                        GetMaintenanceStatusLabel(
                            statusCode)
                });
        }

        return items;
    }

    // =========================================================
    // MAINTENANCE STATUS
    // =========================================================

    private static string GetMaintenanceStatusCode(
        DateTime nextMaintenanceDate,
        DateTime today)
    {
        var maintenanceDate =
            nextMaintenanceDate.Date;

        var todayDate =
            today.Date;

        if (maintenanceDate < todayDate)
        {
            return "OVERDUE";
        }

        if (maintenanceDate <=
            todayDate.AddDays(90))
        {
            return "DUE_SOON";
        }

        return "UP_TO_DATE";
    }

    private static string GetMaintenanceStatusLabel(
        string statusCode)
    {
        return statusCode switch
        {
            "UP_TO_DATE" =>
                "Up to date",

            "DUE_SOON" =>
                "Due soon",

            "OVERDUE" =>
                "Overdue",

            _ =>
                statusCode
        };
    }

    // =========================================================
    // MAINTENANCE FILTERING
    // =========================================================

    private static bool MatchesMaintenanceFilter(
        PortalMaintenanceDashboardItemDto item,
        DashboardMaintenanceFilterType filterType,
        DateTime today)
    {
        var maintenanceDate =
            item.NextMaintenanceDate.Date;

        var todayDate =
            today.Date;

        return filterType switch
        {
            DashboardMaintenanceFilterType.UP_TO_DATE =>
                maintenanceDate >
                todayDate.AddDays(90),

            DashboardMaintenanceFilterType.DUE_SOON =>
                maintenanceDate >= todayDate &&
                maintenanceDate <= todayDate.AddDays(90),

            DashboardMaintenanceFilterType.OVERDUE =>
                maintenanceDate < todayDate,

            DashboardMaintenanceFilterType.WITHIN_7_DAYS =>
                maintenanceDate >= todayDate &&
                maintenanceDate <= todayDate.AddDays(7),

            DashboardMaintenanceFilterType.DAYS_8_TO_14 =>
                maintenanceDate > todayDate.AddDays(7) &&
                maintenanceDate <= todayDate.AddDays(14),

            DashboardMaintenanceFilterType.DAYS_15_TO_30 =>
                maintenanceDate > todayDate.AddDays(14) &&
                maintenanceDate <= todayDate.AddDays(30),

            DashboardMaintenanceFilterType.DAYS_31_TO_90 =>
                maintenanceDate > todayDate.AddDays(30) &&
                maintenanceDate <= todayDate.AddDays(90),

            _ =>
                false
        };
    }

    // =========================================================
    // DATE PARSING
    // =========================================================

    private static bool TryGetMaintenanceDate(
        string? value,
        out DateTime date)
    {
        if (DateTime.TryParse(
            value,
            out var parsed))
        {
            date =
                parsed.Date;

            return true;
        }

        date =
            default;

        return false;
    }
}