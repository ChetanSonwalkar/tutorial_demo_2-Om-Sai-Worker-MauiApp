using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Net.Http.Headers;
using System.Text.Json;
using tutorial_demo_2.Models;
using tutorial_demo_2.Services;
using System.Windows.Input;
using tutorial_demo_2.Pages;

namespace tutorial_demo_2.ViewModels
{
	public partial class DashboardViewModel : ObservableObject
	{
		private readonly IAppStorageService _storageService;
		private readonly HttpClient _httpClient;
		private readonly SessionService _sessionService;


		[ObservableProperty]
		private Worker _worker;

		[ObservableProperty]
		private List<Address> _addresses = new();

		[ObservableProperty]
		private List<DocumentViewModel> _documents = new();

		[ObservableProperty]
		private AddressView _address = new AddressView();

		[ObservableProperty]
		private string _userImage;

		[ObservableProperty]
		private UserDetailsModel _userDetails;

		[ObservableProperty]
		private bool _isBusy;  // Prevent UI blocking

		public ICommand LogoutCommand { get; }
		public ICommand NavigateToChangePasswordCommand { get; }
		public ICommand NavigateToChangeProfileImageCommand { get; }

		public DashboardViewModel(IAppStorageService storageService, HttpClient httpClient, SessionService sessionService)
		{
			_storageService = storageService;
			_httpClient = httpClient;

			LogoutCommand = new RelayCommand(async () => await LogoutAsync());
			NavigateToChangePasswordCommand = new RelayCommand(async () => await NavigateToChangePassword());
			NavigateToChangeProfileImageCommand = new RelayCommand(async () => await NavigateToChangeProfileImage());

			_sessionService = sessionService;
		}

		[RelayCommand]
		private async Task LoadUserDetailsAsync()
		{
			if (IsBusy) return;  // Prevent multiple calls
			IsBusy = true;

			try
			{
				// Get User Details
				var userDetails = await _storageService.GetUserAsync();
				if (userDetails == null)
				{
					await Shell.Current.GoToAsync("//LoginPage", true);
					return;
				}

				// Get Worker ID
				var workerId = await _storageService.GetWorkerIdAsync();
				if (!workerId.HasValue)
				{
					await Shell.Current.GoToAsync("//LoginPage", true);
					return;
				}

				// Set Authorization Token
				var token = await _storageService.GetTokenAsync();
				if (!string.IsNullOrEmpty(token))
				{
					_httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
				}

				// Run API call in the background to prevent UI freezing
				var response = await Task.Run(async () =>
				{
					return await _httpClient.GetAsync($"{ApiUrls.GetWorkerProfileEndpoint}{workerId.Value}");
				});

				if (response.IsSuccessStatusCode)
				{
					var jsonString = await response.Content.ReadAsStringAsync();
					System.Diagnostics.Debug.WriteLine($"Raw JSON response: {jsonString}");

					// Deserialize JSON in the background
					var profile = await Task.Run(() =>
						JsonSerializer.Deserialize<WorkerProfileResponse>(jsonString, new JsonSerializerOptions
						{
							PropertyNameCaseInsensitive = true,
							DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
						})
					);

					// UI Updates on Main Thread
					MainThread.BeginInvokeOnMainThread(() =>
					{
						if (profile?.Data != null)
						{
							Worker = profile.Data.Worker;
							UpdateAddresses(profile.Data.Addresses);
							UpdateDocuments(profile.Data.Documents);
							UpdateProfileImage(userDetails.ProfileImage);
						}
					});
				}
				else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
				{
					await Shell.Current.GoToAsync("//LoginPage", true);
				}
				else
				{
					System.Diagnostics.Debug.WriteLine("Failed to load profile.");
				}
			}
			catch (HttpRequestException ex)
			{
				System.Diagnostics.Debug.WriteLine($"Network error: {ex.Message}");
			}
			catch (JsonException ex)
			{
				System.Diagnostics.Debug.WriteLine($"JSON Deserialization Error: {ex.Message}");
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"Unexpected error: {ex.Message}");
			}
			finally
			{
				IsBusy = false; // Hide loader after API call
			}
		}

		// Method to update addresses
		private void UpdateAddresses(List<Address>? addresses)
		{
			if (addresses != null && addresses.Any())
			{
				var permanentAddress = addresses.FirstOrDefault(a => a.AddressType == "Permanent")?.AddressText ?? "Not Provided";
				var currentAddress = addresses.FirstOrDefault(a => a.AddressType == "Current")?.AddressText ?? "Not Provided";

				Address = new AddressView
				{
					PermanentAddress = permanentAddress,
					CurrentAddress = currentAddress
				};
			}
		}

		// Method to update documents
		private void UpdateDocuments(List<Document>? documents)
		{
			if (documents != null)
			{
				Documents = documents.Select(d => new DocumentViewModel
				{
					DocumentName = d.DocumentName,
					Status = d.IsVerified ? "Verified" : "Pending",
					StatusColor = d.IsVerified ? Colors.Green : Colors.Orange
				}).ToList();

				System.Diagnostics.Debug.WriteLine($"Documents Loaded: {Documents.Count}");
			}
		}

		// Method to update profile image
		private void UpdateProfileImage(string? profileImagePath)
		{
			if (!string.IsNullOrEmpty(profileImagePath))
			{
				try
				{
					var imagePath = profileImagePath.TrimStart('/');
					if (!imagePath.StartsWith("media/profile_images/"))
					{
						imagePath = $"media/profile_images/{imagePath}";
					}

					UserImage = $"{ApiUrls.ImageUrl.TrimEnd('/')}/{imagePath}";
					System.Diagnostics.Debug.WriteLine($"Setting profile image URL: {UserImage}");
				}
				catch (Exception ex)
				{
					System.Diagnostics.Debug.WriteLine($"Error setting image source: {ex}");
					UserImage = "man.png"; // Fallback image
				}
			}
			else
			{
				UserImage = "man.png"; // Default placeholder image
			}
		}

		private async Task NavigateToChangePassword()
		{
			await Shell.Current.GoToAsync(nameof(ChangePasswordPage), true);
		}

		private async Task NavigateToChangeProfileImage()
		{
			await Shell.Current.GoToAsync(nameof(ChangeProfileImagePage), true);
		}

		//private async Task LogoutAsync()
		//{
		//	bool result = await Application.Current.MainPage.DisplayAlert("Confirm", "Are you sure you want to logout?", "Yes", "No");
		//	if (result)
		//	{
		//		await _storageService.ClearStorageAsync();
		//		await Shell.Current.GoToAsync("//LoginPage", true);
		//	}
		//}

		private async Task LogoutAsync()
		{
			bool result = await Application.Current.MainPage.DisplayAlertAsync("Confirm", "Are you sure you want to logout?", "Yes", "No");
			if (result)
			{
				await _storageService.ClearStorageAsync();

				_sessionService.Remove("SiteId");
				_sessionService.Remove("SiteName");
				_sessionService.Remove("GpsLocation");


				// Reset UI properties
				var appShell = (AppShell)Shell.Current;
				appShell.IsSiteListVisible = false;
				appShell.IsAttendanceVisible = false;

				appShell.ResetUserPermissions(); // Reset UI properties

				await Shell.Current.GoToAsync("//LoginPage", true);
			}
		}


	}
}
