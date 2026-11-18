using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using tutorial_demo_2.Models;

namespace tutorial_demo_2.Services
{
	public interface IWorkerService
	{
		Task<WorkerListResponse<WorkerListData>> WorkerListAsync(int siteId);
		Task<ChangePasswordResponse> ChangePasswordAsync(ChangePasswordRequest request);
	}

	public class WorkerService : IWorkerService
	{
		private readonly HttpClient _httpClient;
		private readonly IAppSecureStorage _secureStorage;

		public WorkerService(HttpClient httpClient, IAppSecureStorage secureStorage)
		{
			_httpClient = httpClient;
			_secureStorage = secureStorage;
		}

		public async Task<WorkerListResponse<WorkerListData>> WorkerListAsync(int siteId)
		{
			var token = await _secureStorage.GetAuthTokenAsync();
			if (string.IsNullOrEmpty(token))
			{
				await _secureStorage.RemoveAllAsync();
				await Shell.Current.GoToAsync("//LoginPage", true);
				return UnauthorizedResponse();
			}

			_httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
			var response = await _httpClient.GetAsync($"{ApiUrls.GetWorkerBySiteIdEndpoint}{siteId}");

			if (!response.IsSuccessStatusCode)
				return ErrorResponse("Worker not found!");

            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var apiResponse = await response.Content.ReadFromJsonAsync<
                WorkerListResponse<WorkerListData>>(jsonOptions);

            System.Diagnostics.Debug.WriteLine(
                $"Worker API Response: {JsonSerializer.Serialize(apiResponse)}");

            return apiResponse ?? ErrorResponse("Invalid worker API response.");

           
		}

		public async Task<ChangePasswordResponse> ChangePasswordAsync(ChangePasswordRequest request)
		{
			try
			{
				var token = await _secureStorage.GetAuthTokenAsync();
				if (string.IsNullOrEmpty(token))
				{
					return new ChangePasswordResponse { Success = false, Message = "User is not logged in." };
				}

				_httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
				var json = JsonSerializer.Serialize(request);
				var content = new StringContent(json, Encoding.UTF8, "application/json");
				var response = await _httpClient.PostAsync(ApiUrls.PostChangePasswordEndpoint, content);

				return response.IsSuccessStatusCode
					? await response.Content.ReadFromJsonAsync<ChangePasswordResponse>()
					: new ChangePasswordResponse { Success = false, Message = "Failed to change password." };
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"Error: {ex.Message}");
				return new ChangePasswordResponse { Success = false, Message = "An error occurred. Please try again." };
			}
		}

		private static WorkerListResponse<WorkerListData> UnauthorizedResponse() =>
			new WorkerListResponse<WorkerListData> { Success = false, Errors = new Dictionary<string, List<string>> { { "Authentication", new List<string> { "User is not logged in." } } } };

		private static WorkerListResponse<WorkerListData> ErrorResponse(string message) =>
			new WorkerListResponse<WorkerListData> { Success = false, Errors = new Dictionary<string, List<string>> { { "Worker", new List<string> { message } } } };
	}
}
