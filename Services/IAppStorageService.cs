using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tutorial_demo_2.Models;

namespace tutorial_demo_2.Services
{
    public interface IAppStorageService
	{
		Task<string> GetTokenAsync();
		Task SaveTokenAsync(string token);
		Task<UserDetailsModel> GetUserAsync();
		Task SaveUserAsync(UserDetailsModel user);
		Task<int?> GetWorkerIdAsync();
		Task SaveWorkerIdAsync(int workerId);
		Task ClearStorageAsync();
	}
}
