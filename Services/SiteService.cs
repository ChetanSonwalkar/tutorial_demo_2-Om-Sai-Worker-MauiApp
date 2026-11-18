using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using tutorial_demo_2.Models;

namespace tutorial_demo_2.Services
{
	public interface ISiteService
	{
		Task<SiteResponse> GetSitesAsync();
	}

	public class SiteService : ISiteService
	{
		private readonly HttpClient _httpClient;
		private readonly IAppSecureStorage _secureStorage;

		public SiteService(HttpClient httpClient, IAppSecureStorage secureStorage)
		{
			_httpClient = httpClient;
			_secureStorage = secureStorage;
		}

		public async Task<SiteResponse> GetSitesAsync()
		{
			return await Task.Run(async () =>
			{
				var token = await _secureStorage.GetAuthTokenAsync();

				if (string.IsNullOrEmpty(token))
				{
					await HandleUnauthorizedUser();
					return UnauthorizedResponse();
				}

				_httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

				try
				{
					var response = await _httpClient.GetAsync(ApiUrls.GetSiteListEndpoint);

					if (!response.IsSuccessStatusCode)
					{
						return new SiteResponse
						{
							Success = false,
							Errors = new Dictionary<string, List<string>>
							{
								{ "API", new List<string> { $"Error: {response.StatusCode}" } }
							}
						};
					}

					var apiResponse = await response.Content.ReadFromJsonAsync<SiteResponse>();

					return apiResponse ?? UnauthorizedResponse();
				}
				catch (Exception ex)
				{
					return new SiteResponse
					{
						Success = false,
						Errors = new Dictionary<string, List<string>>
						{
							{ "Exception", new List<string> { ex.Message } }
						}
					};
				}
			});
		}

		private async Task HandleUnauthorizedUser()
		{
			await MainThread.InvokeOnMainThreadAsync(async () =>
			{
				await Shell.Current.DisplayAlertAsync("Error", "Unauthorized user! Please login again.", "OK");
				await _secureStorage.RemoveAllAsync();
				await Shell.Current.GoToAsync("//LoginPage", true);
			});
		}

		private SiteResponse UnauthorizedResponse() => new()
		{
			Success = false,
			Errors = new Dictionary<string, List<string>>
			{
				{ "Authentication", new List<string> { "User is not logged in." } }
			}
		};
	}
}
