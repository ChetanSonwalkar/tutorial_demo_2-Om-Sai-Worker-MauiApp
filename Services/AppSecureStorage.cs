using System.Text.Json;
using tutorial_demo_2.Models;
using tutorial_demo_2.Services;
using Microsoft.Maui.Storage; // Ensure Preferences is available

namespace tutorial_demo_2.Services
{
	public class AppSecureStorage : IAppSecureStorage
	{
		private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
		private const string USER_DETAILS_KEY = "user_details";
		private const string AUTH_TOKEN_KEY = "auth_token";
		private const string WORKER_ID_KEY = "worker_id";
		private const string WORKMAN_ID_KEY = "workman_id";
		private const string SITE_ID_KEY = "site_id";

		public async Task<UserDetailsModel> GetUserDetailsAsync()
		{
			try
			{
				await _semaphore.WaitAsync();
				var json = Preferences.Default.Get(USER_DETAILS_KEY, string.Empty);
				if (string.IsNullOrEmpty(json))
					return null;
				return JsonSerializer.Deserialize<UserDetailsModel>(json);
			}
			catch (Exception)
			{
				return null;
			}
			finally
			{
				_semaphore.Release();
			}
		}

		public async Task SetUserDetailsAsync(UserDetailsModel userDetails)
		{
			try
			{
				await _semaphore.WaitAsync();
				if (userDetails == null)
				{
					Preferences.Default.Remove(USER_DETAILS_KEY);
					return;
				}

				var json = JsonSerializer.Serialize(userDetails);
				Preferences.Default.Set(USER_DETAILS_KEY, json);
				Preferences.Default.Set(WORKER_ID_KEY, userDetails.WorkerId);
				Preferences.Default.Set(WORKMAN_ID_KEY, userDetails.WorkmanId);
				Preferences.Default.Set(SITE_ID_KEY, userDetails.SiteId);
			}
			catch (Exception)
			{
				// Log error or handle appropriately
			}
			finally
			{
				_semaphore.Release();
			}
		}

		public async Task<string> GetAuthTokenAsync()
		{
			try
			{
				await _semaphore.WaitAsync();
				return Preferences.Default.Get(AUTH_TOKEN_KEY, string.Empty);
			}
			catch (Exception)
			{
				return null;
			}
			finally
			{
				_semaphore.Release();
			}
		}

		public async Task SetAuthTokenAsync(string token)
		{
			try
			{
				await _semaphore.WaitAsync();
				if (string.IsNullOrEmpty(token))
				{
					Preferences.Default.Remove(AUTH_TOKEN_KEY);
					return;
				}
				Preferences.Default.Set(AUTH_TOKEN_KEY, token);
			}
			catch (Exception)
			{
				// Log error or handle appropriately
			}
			finally
			{
				_semaphore.Release();
			}
		}

		public Task RemoveAllAsync()
		{
			try
			{
				Preferences.Default.Remove(USER_DETAILS_KEY);
				Preferences.Default.Remove(AUTH_TOKEN_KEY);
				Preferences.Default.Remove(WORKER_ID_KEY);
				Preferences.Default.Remove(WORKMAN_ID_KEY);
				Preferences.Default.Remove(SITE_ID_KEY);
				return Task.CompletedTask;
			}
			catch (Exception)
			{
				return Task.CompletedTask;
			}
		}
	}
}
