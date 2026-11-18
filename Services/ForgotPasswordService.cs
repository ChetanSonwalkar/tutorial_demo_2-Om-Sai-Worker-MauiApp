using System.Net.Http.Json;
using tutorial_demo_2.Models;

namespace tutorial_demo_2.Services;

public class ForgotPasswordService : IForgotPasswordService
{
    private readonly HttpClient _httpClient;

    private const string Endpoint = "api/Account/forgot-password";

    public ForgotPasswordService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ForgotPasswordResponse> ForgotPasswordAsync(
        ForgotPasswordRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync(
            Endpoint,
            request);

        var result = await response.Content
            .ReadFromJsonAsync<ForgotPasswordResponse>();

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                result?.Message ??
                $"Request failed with status code {(int)response.StatusCode}.");
        }

        return result ?? new ForgotPasswordResponse
        {
            Success = false,
            Message = "No response received from server."
        };
    }
}