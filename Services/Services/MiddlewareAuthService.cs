using System.Net.Http.Json;
using mysystem_bff.Models.Admin;
using mysystem_bff.Models.Middleware;
using mysystem_bff.Services.Interfaces;

namespace mysystem_bff.Services.Services;

public class MiddlewareAuthService : IMiddlewareAuthService
{
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;

    public MiddlewareAuthService(
        IConfiguration configuration,
        HttpClient httpClient)
    {
        _configuration = configuration;
        _httpClient = httpClient;
    }

    public async Task<ServiceResult<string>> GetMiddlewareToken()
    {
        var baseUrl = _configuration["MiddlewareApi:BaseUrl"];
        var username = _configuration["MiddlewareApi:Username"];
        var password = _configuration["MiddlewareApi:Password"];

        if (
            string.IsNullOrWhiteSpace(baseUrl) ||
            string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrWhiteSpace(password)
        )
        {
            return ServiceResult<string>.Fail(
                "Middleware API configuration is missing.",
                500
            );
        }

        var loginResponse = await _httpClient.PostAsJsonAsync(
            $"{baseUrl}/api/auth/login",
            new
            {
                username,
                password
            }
        );

        if (!loginResponse.IsSuccessStatusCode)
        {
            return ServiceResult<string>.Fail(
                "Failed to authenticate with middleware API.",
                502
            );
        }

        var tokenResponse =
            await loginResponse.Content.ReadFromJsonAsync<MiddlewareTokenResponse>();

        if (tokenResponse is null || string.IsNullOrWhiteSpace(tokenResponse.AccessToken))
        {
            return ServiceResult<string>.Fail(
                "Middleware API returned an invalid token response.",
                502
            );
        }

        return ServiceResult<string>.Ok(tokenResponse.AccessToken);
    }
}