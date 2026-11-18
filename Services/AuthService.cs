using System.Net.Http;
using System.Net.Http.Json;
using tutorial_demo_2.Models;

namespace tutorial_demo_2.Services
{
	public interface ILoginService
	{
		Task<LoginResponse> LoginAsync(LoginRequest request);
	}

	public class LoginService : ILoginService
	{
		private readonly HttpClient _httpClient;
		private readonly IHttpClientFactory _httpClientFactory;

		public LoginService(HttpClient httpClient, IHttpClientFactory httpClientFactory)
		{
			_httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
			_httpClientFactory = httpClientFactory;
		}

		public async Task<LoginResponse> LoginAsync(LoginRequest request)
		{
	
			try
			{

				using var response = await _httpClient.PostAsJsonAsync(ApiUrls.PostLoginEndpoint, request);

				if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
				{
					return CreateErrorResponse("Invalid login credentials.");
				}

				if (!response.IsSuccessStatusCode)
				{
					return CreateErrorResponse($"Error: {response.StatusCode}");
				}

				var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();
				return loginResponse ?? CreateErrorResponse("Invalid server response.");
			}
			catch (HttpRequestException ex)
			{
				return CreateErrorResponse($"Network error: {ex.Message}");
			}
			catch (TaskCanceledException)
			{
				return CreateErrorResponse("Request timed out. Please try again.");
			}
			catch (Exception ex)
			{
				return CreateErrorResponse($"Unexpected error: {ex.Message}");
			}
		}

		private static LoginResponse CreateErrorResponse(string message)
		{
			return new LoginResponse
			{
				Success = false,
				Errors = new Dictionary<string, List<string>> { { "workmanId", new List<string> { message } } }
			};
		}
	}
}
