using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using tutorial_demo_2.Models;
using tutorial_demo_2.Services;

namespace tutorial_demo_2.Services
{
	public interface IAttendanceService
	{
		Task<ApiResponseListData<AttendanceRecord>> LastAttendanceAsync(int workerId);
		Task<ApiResponseListData<AttendanceRecord>> GetAllAttendanceAsync(int workerId);
	}

	public class AttendanceService : IAttendanceService
	{
		private readonly HttpClient _httpClient;
		private readonly IAppSecureStorage _secureStorage;

		public AttendanceService(HttpClient httpClient, IAppSecureStorage secureStorage)
		{
			_httpClient = httpClient;
			_secureStorage = secureStorage;
		}

		public async Task<ApiResponseListData<AttendanceRecord>> LastAttendanceAsync(int workerId)
		{
			return await GetAttendanceAsync(workerId, 1);
		}

		public async Task<ApiResponseListData<AttendanceRecord>> GetAllAttendanceAsync(int workerId)
		{
			return await GetAttendanceAsync(workerId, 100);
		}

		private async Task<ApiResponseListData<AttendanceRecord>> GetAttendanceAsync(int workerId, int recordCount)
		{
			var token = await _secureStorage.GetAuthTokenAsync();

			if (string.IsNullOrEmpty(token))
			{
				await HandleUnauthorizedUser();
				return CreateErrorResponse("Authentication", "User is not logged in.");
			}

			_httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
			_httpClient.DefaultRequestHeaders.Accept.Clear();
			_httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

			var requestBody = new { WorkerId = workerId, RecordCount = recordCount };
			var jsonPayload = JsonSerializer.Serialize(requestBody);
			var httpContent = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

			var response = await _httpClient.PostAsync(ApiUrls.PostAttendanceEndpoint(), httpContent);

			if (response.IsSuccessStatusCode)
			{
				var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponseListData<AttendanceRecord>>();
				if (apiResponse?.Data != null && apiResponse.Data.Any())
				{
					return new ApiResponseListData<AttendanceRecord>
					{
						Success = true,
						Data = recordCount == 1 ? new List<AttendanceRecord> { apiResponse.Data.First() } : apiResponse.Data,
						Errors = null
					};
				}
			}

			return CreateErrorResponse("WorkerId", "Attendance record not found!");
		}

		private async Task HandleUnauthorizedUser()
		{
			await MainThread.InvokeOnMainThreadAsync(async () =>
			{
				await Shell.Current.DisplayAlertAsync("Error", "Unauthorized user! Please login again.", "OK");
			});
			await _secureStorage.RemoveAllAsync();
			await Shell.Current.GoToAsync("//LoginPage", true);
		}

		private ApiResponseListData<AttendanceRecord> CreateErrorResponse(string key, string errorMessage)
		{
			return new ApiResponseListData<AttendanceRecord>
			{
				Success = false,
				Data = null,
				Errors = new Dictionary<string, List<string>>
				{
					{ key, new List<string> { errorMessage } }
				}
			};
		}
	}
}