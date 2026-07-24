using System.Net.Http.Headers;
using System.Net.Http.Json;
using mysystem_bff.Models.Admin;
using mysystem_bff.Models.Middleware;
using mysystem_bff.Models.Portal;
using mysystem_bff.Services.Interfaces;

namespace mysystem_bff.Services.Services
{
    public class MiddlewareSiteSystemsService : IMiddlewareSiteSystemsService
    {
        private readonly IConfiguration _config;
        private readonly HttpClient _httpClient;
        private readonly IMiddlewareAuthService _authService;

        public MiddlewareSiteSystemsService(
            IConfiguration config,
            HttpClient httpClient,
            IMiddlewareAuthService authService)
        {
            _config = config;
            _httpClient = httpClient;
            _authService = authService;
            _authService = authService;
        }

        public async Task<ServiceResult<PortalSiteSystemsResponse>> GetSiteSystemsAsync(
            PortalSiteSystemsQuery query,
            CancellationToken ct)
        {
            var siteId = query.SiteId?.Trim().ToUpperInvariant();

            if (string.IsNullOrWhiteSpace(siteId))
            {
                return ServiceResult<PortalSiteSystemsResponse>.Fail(
                    "Site ID is required.",
                    400);
            }

            var token = await _authService.GetMiddlewareToken();

            if (!token.Success ||
                string.IsNullOrWhiteSpace(token.Data))
            {
                return ServiceResult<PortalSiteSystemsResponse>.Fail(
                    token.Error ?? "Unable to authenticate with middleware API.",
                    token.StatusCode);
            }

            var baseUrl = _config["MiddlewareApi:baseUrl"];
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                return ServiceResult<PortalSiteSystemsResponse>.Fail(
                    "Middleware API base URL is missing.",
                    500);
            }

            var page = (query.Page > 0)
                ? query.Page
                : 1;
            var pageSize = Math.Clamp(query.PageSize, 1, 100);

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"{baseUrl}/api/site-systems?siteId={siteId}&page={page}&pageSize={pageSize}");

            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", token.Data);

            using var response = await _httpClient.SendAsync(request, ct);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct);
                return ServiceResult<PortalSiteSystemsResponse>.Fail(
                    string.IsNullOrWhiteSpace(error)
                        ? "Middleware site systems request failed."
                        : error,
                    (int)response.StatusCode);
            }

            var middlewareResponse =
                await response.Content
                    .ReadFromJsonAsync<MiddlewareSiteSystemsResponse>(
                        cancellationToken: ct);

            if (middlewareResponse is null)
            {
                return ServiceResult<PortalSiteSystemsResponse>.Fail(
                    "Middleware API returned an invalid site systems response.",
                    502);
            }

            var result = new PortalSiteSystemsResponse
            {
                Items = middlewareResponse.Items
                    .Select(MapSiteSystem)
                    .ToList(),

                Page = middlewareResponse.Page,
                PageSize = middlewareResponse.PageSize,
                Total = middlewareResponse.Total,
                HasMore = middlewareResponse.HasMore
            };

            return ServiceResult<PortalSiteSystemsResponse>.Ok(result);
        }

        private static PortalSiteSystemDto MapSiteSystem(
            MiddlewareSiteSystem ss)
        {
            return new PortalSiteSystemDto
            {
                SystemNo = ss.SystemNo,
                SiteId = ss.SiteId,
                SystemCode = ss.SystemCode,
                Status = ss.Status,
                Maintained_YN = ss.Maintained_YN,
                CommissionedDate = ss.CommissionedDate,
                LastMaintenanceDate = ss.LastMaintenanceDate,
                NextMaintenanceDate = ss.NextMaintenanceDate
            };
        }
    }
}
