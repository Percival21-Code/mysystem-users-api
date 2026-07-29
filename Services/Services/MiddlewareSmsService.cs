using System.Net.Http.Json;
using System.Net.Http.Headers;
using mysystem_bff.Models.Admin;
using mysystem_bff.Models.Middleware;
using mysystem_bff.Models.Portal;
using mysystem_bff.Services.Interfaces;

namespace mysystem_bff.Services.Services
{
    public class MiddlewareSmsService : IMiddlewareSmsService
    {
        private readonly IConfiguration _config;
        private readonly IMiddlewareAuthService _authService;
        private readonly HttpClient _httpClient;

        public MiddlewareSmsService(
            IConfiguration config,
            IMiddlewareAuthService authService,
            HttpClient httpClient)
        {
            _config = config;
            _authService = authService;
            _httpClient = httpClient;
        }

        public async Task<ServiceResult<PortalSMSResponse>> GetSms(
            PortalSMSQuery query,
            CancellationToken ct = default)
        {
            // data

            var cleanSiteId = query.SiteId?.Trim().ToUpperInvariant() ?? "";
            if (query.SystemNo <= 0) { query.SystemNo = 1; }

            if (string.IsNullOrWhiteSpace(cleanSiteId))
            {
                return ServiceResult<PortalSMSResponse>.Fail(
                "Site ID is required.",
                400);
            }

            // fetch auth token

            var token = await _authService.GetMiddlewareToken();

            if (!token.Success || string.IsNullOrWhiteSpace(token.Data))
            {
                return ServiceResult<PortalSMSResponse>.Fail(
                    "Could not obtain middleware authentication",
                    500);
            }

            // get base url

            var baseUrl = _config["MiddlewareApi:BaseUrl"];

            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                return ServiceResult<PortalSMSResponse>.Fail(
                    "Middleware API base URL is missing.",
                    500);
            }

            // set up parameters from query

            var parameters = new Dictionary<string, string?>
            {
                ["siteId"] = cleanSiteId,
                ["systemNo"] = query.SystemNo.ToString()
            };

            var queryString = string.Join(
            "&",
            parameters
                .Where(parameter =>
                    !string.IsNullOrWhiteSpace(parameter.Value))
                .Select(parameter =>
                    $"{Uri.EscapeDataString(parameter.Key)}=" +
                    $"{Uri.EscapeDataString(parameter.Value!)}"));

            // set up and send data request

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"{baseUrl}/api/system-maint-schedules?{queryString}");
            request.Headers.Authorization = new AuthenticationHeaderValue(
                "Bearer", token.Data);

            using var response = await _httpClient.SendAsync(request, ct);
            
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(
                ct);

                return ServiceResult<PortalSMSResponse>.Fail(
                    string.IsNullOrWhiteSpace(error)
                        ? "Middleware systems maintenance schedules request failed."
                        : error,
                    (int)response.StatusCode);
            }

            // convert response as readable

            var middlewareResponse =
                await response.Content
                    .ReadFromJsonAsync<MiddlewareSMSResponse>(
                        cancellationToken: ct);

            if (middlewareResponse is null)
            {
                return ServiceResult<PortalSMSResponse>.Fail(
                    "Middleware returned an invalid response",
                    500);
            }

            var result = new PortalSMSResponse
            {
                Items = middlewareResponse.Items
                    .Select(MapSms)
                    .ToList(),
                Page = middlewareResponse.Page,
                PageSize = middlewareResponse.PageSize,
                Total = middlewareResponse.Total,
                HasMore = middlewareResponse.HasMore
            };

            return ServiceResult<PortalSMSResponse>.Ok(result);
        }

        private static PortalSMSDto MapSms(
            MiddlewareSMS sms)
        {
            return new PortalSMSDto
            {
                SiteId = sms.SiteId ?? "",
                SystemNo = (sms.SystemNo < 1) 
                    ? 1
                    : sms.SystemNo,
                NextMaintenanceDate = sms.NextMaintenanceDate ?? "",
                Description = sms.Description ?? ""
            };
        }
    }
}
