using System;
using System.Text.Json;
using System.Threading.Tasks;
using tutorial_demo_2.Models;
using Microsoft.Maui.Storage;

namespace tutorial_demo_2.Services
{
	public class AppStorageService : IAppStorageService
	{
		private const string TokenKey = "auth_token";
		private const string UserKey = "user_details";
		private const string WorkerIdKey = "worker_id";

		public Task SaveTokenAsync(string token)
		{
			if (string.IsNullOrEmpty(token))
			{
				Preferences.Default.Remove(TokenKey);
				return Task.CompletedTask;
			}

			Preferences.Default.Set(TokenKey, token);
			return Task.CompletedTask;
		}

		public Task<string> GetTokenAsync()
		{
			return Task.FromResult(Preferences.Default.Get(TokenKey, string.Empty));
		}

		public Task SaveUserAsync(UserDetailsModel user)
		{
			if (user == null)
			{
				Preferences.Default.Remove(UserKey);
				return Task.CompletedTask;
			}

			var userJson = JsonSerializer.Serialize(user);
			Preferences.Default.Set(UserKey, userJson);
			return Task.CompletedTask;
		}

		public Task<UserDetailsModel> GetUserAsync()
		{
			var userJson = Preferences.Default.Get(UserKey, string.Empty);
			return Task.FromResult(string.IsNullOrEmpty(userJson) ? null : JsonSerializer.Deserialize<UserDetailsModel>(userJson));
		}

		public async Task<int?> GetWorkerIdAsync()
		{
			var user = await GetUserAsync();
			if (user?.WorkerId > 0)
			{
				return user.WorkerId;
			}

			return Preferences.Default.ContainsKey(WorkerIdKey) ? Preferences.Default.Get(WorkerIdKey, 0) : (int?)null;
		}

		public async Task SaveWorkerIdAsync(int workerId)
		{
			if (workerId <= 0)
			{
				Preferences.Default.Remove(WorkerIdKey);
				return;
			}

			var user = await GetUserAsync();
			if (user != null)
			{
				user.WorkerId = workerId;
				await SaveUserAsync(user);
			}

			Preferences.Default.Set(WorkerIdKey, workerId);
		}

        public Task ClearStorageAsync()
        {
            Preferences.Default.Remove(TokenKey);
            Preferences.Default.Remove(UserKey);
            Preferences.Default.Remove(WorkerIdKey);

            return Task.CompletedTask;
        }
    }
}
