using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using tutorial_demo_2.Models;

namespace tutorial_demo_2.Services
{

	public interface ILeaveRequestService
	{
		Task<List<LeaveTypeModel>> GetLeaveTypesAsync();
		Task<List<LeaveRequestModel>> GetWorkerLeavesAsync(int workerId);
		Task<bool> CreateLeaveRequestAsync(LeaveRequestCreateModel leaveRequest);
		Task<bool> DeleteLeaveRequestAsync(int leaveRequestId);
	}

	public class LeaveRequestService : ILeaveRequestService
	{
		private readonly HttpClient _httpClient;
		private readonly IAppSecureStorage _secureStorage;

		public LeaveRequestService(IAppSecureStorage secureStorage)
		{
			_httpClient = new HttpClient();
			_secureStorage = secureStorage;
		}

		private async Task SetAuthorizationHeader()
		{
			var token = await _secureStorage.GetAuthTokenAsync();
			if (!string.IsNullOrEmpty(token))
			{
				_httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
			}
		}

		public async Task<List<LeaveTypeModel>> GetLeaveTypesAsync()
		{
			try
			{
				await SetAuthorizationHeader();
				var response = await _httpClient.GetAsync(ApiUrls.GetLeaveTypesEndpoint);
				response.EnsureSuccessStatusCode();

				var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponseModel<List<LeaveTypeModel>>>();
				if (apiResponse?.Success != true || apiResponse.Data == null)
				{
					return new List<LeaveTypeModel>();
				}

				return apiResponse.Data.GetData() ?? new List<LeaveTypeModel>();
			}
			catch (HttpRequestException ex)
			{
				Console.WriteLine($"HTTP request failed: {ex.Message}");
				throw;
			}
			catch (JsonException ex)
			{
				Console.WriteLine($"JSON parsing failed: {ex.Message}");
				throw;
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Unexpected error: {ex.Message}");
				throw;
			}
		}

		public async Task<List<LeaveRequestModel>> GetWorkerLeavesAsync(int workerId)
		{
			try
			{
				await SetAuthorizationHeader();
				var response = await _httpClient.GetAsync($"{ApiUrls.GetWorkerLeavesEndpoint}{workerId}");
				response.EnsureSuccessStatusCode();

				var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponseModel<List<LeaveRequestModel>>>();
				if (apiResponse?.Success != true || apiResponse.Data == null)
				{
					return new List<LeaveRequestModel>();
				}

				return apiResponse.Data.GetData() ?? new List<LeaveRequestModel>();
			}
			catch (HttpRequestException ex)
			{
				Console.WriteLine($"HTTP request failed: {ex.Message}");
				throw;
			}
			catch (JsonException ex)
			{
				Console.WriteLine($"JSON parsing failed: {ex.Message}");
				throw;
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Unexpected error: {ex.Message}");
				throw;
			}
		}

		public async Task<bool> CreateLeaveRequestAsync(LeaveRequestCreateModel leaveRequest)
		{
			try
			{
				await SetAuthorizationHeader();

				// Ensure required fields are set
				if (leaveRequest.WorkerId <= 0)
				{
					Console.WriteLine("WorkerId is required");
					return false;
				}
				if (leaveRequest.LeaveTypeId <= 0)
				{
					Console.WriteLine("LeaveTypeId is required");
					return false;
				}
				if (string.IsNullOrEmpty(leaveRequest.Reason))
				{
					Console.WriteLine("Reason is required");
					return false;
				}
				if (string.IsNullOrEmpty(leaveRequest.StartDate))
				{
					Console.WriteLine("StartDate is required");
					return false;
				}
				if (string.IsNullOrEmpty(leaveRequest.EndDate))
				{
					Console.WriteLine("EndDate is required");
					return false;
				}

				var response = await _httpClient.PostAsJsonAsync(ApiUrls.PostWorkerLeavesEndpoint, leaveRequest);

				if (!response.IsSuccessStatusCode)
				{
					var rawContent = await response.Content.ReadAsStringAsync();
					Console.WriteLine($"Error Response: {rawContent}");
					Console.WriteLine($"Status Code: {response.StatusCode}, Reason: {response.ReasonPhrase}");

					try
					{
						var errorContent = await response.Content.ReadFromJsonAsync<ApiDirectResponseModel<object>>();
						if (errorContent?.Errors != null)
						{
							Console.WriteLine($"API Error: {errorContent.Errors}");
						}
					}
					catch (JsonException ex)
					{
						// Already logged raw content above
					}
					return false;
				}

				var apiResponse = await response.Content.ReadFromJsonAsync<ApiDirectResponseModel<int>>();
				return apiResponse?.Success ?? false;
			}
			catch (HttpRequestException ex)
			{
				Console.WriteLine($"HTTP request failed: {ex.Message}");
				return false;
			}
			catch (JsonException ex)
			{
				Console.WriteLine($"JSON parsing failed: {ex.Message}");
				return false;
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Unexpected error: {ex.Message}");
				return false;
			}
		}

		public async Task<bool> DeleteLeaveRequestAsync(int leaveRequestId)
		{
			try
			{
				await SetAuthorizationHeader();
				var response = await _httpClient.PostAsync($"{ApiUrls.PostWorkerLeavesDeleteEndpoint}{leaveRequestId}", null);
				response.EnsureSuccessStatusCode();

				var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponseModel<object>>();
				return apiResponse?.Success == true;
			}
			catch (Exception ex)
			{
				// Handle or log the exception as needed
				return false;
			}
		}
	}


}
