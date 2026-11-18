using tutorial_demo_2.Models;

namespace tutorial_demo_2.Services
{
    public interface IAppSecureStorage
	{
		Task<UserDetailsModel> GetUserDetailsAsync();
		Task SetUserDetailsAsync(UserDetailsModel userDetails);
		Task<string> GetAuthTokenAsync();
		Task SetAuthTokenAsync(string token);
		Task RemoveAllAsync();

	}
}
