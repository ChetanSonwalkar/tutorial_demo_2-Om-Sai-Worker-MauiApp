//using CommunityToolkit.Mvvm.ComponentModel;
//using CommunityToolkit.Mvvm.Input;
//using System;
//using System.Collections.Generic;
//using System.Collections.ObjectModel;
//using System.Linq;
//using System.Threading.Tasks;
//using tutorial_demo_2.Models;
//using tutorial_demo_2.Pages;
//using tutorial_demo_2.Services;

//namespace tutorial_demo_2.ViewModels
//{
//	public partial class SiteListViewModel : ObservableObject
//	{
//		private List<SiteModel> allSites = new List<SiteModel>();
//		private readonly ISiteService _siteService;
//		private readonly IAppSecureStorage _secureStorage;
//		public ObservableCollection<SiteModel> FilteredSites { get; set; } = new ObservableCollection<SiteModel>();


//		private int currentChunkIndex = 0;
//		private const int ChunkSize = 20; // Load 20 items per chunk
//		private bool isFetchingMore = false;

//		[ObservableProperty]
//		private string searchText;

//		private bool _isLoading;
//		public bool IsLoading
//		{
//			get => _isLoading;
//			set
//			{
//				_isLoading = value;
//				OnPropertyChanged(nameof(IsLoading));
//			}
//		}

//		public SiteListViewModel(ISiteService siteService, IAppSecureStorage secureStorage)
//		{
//			_siteService = siteService ?? throw new ArgumentNullException(nameof(siteService));
//			_secureStorage = secureStorage ?? throw new ArgumentNullException(nameof(secureStorage));
//			Task.Run(LoadSitesAsync); // Fetch all data once

//			//LoadSites();
//		}

//		private async Task LoadSitesAsync()
//		{
//			IsLoading = true;
//			try
//			{
//				var response = await _siteService.GetSitesAsync();
//				if (response?.Success == true && response.Data != null)
//				{
//					allSites = response.Data; // Store all data in memory
//					LoadMoreSites(); // Load first chunk
//				}
//				else
//				{
//					await Shell.Current.DisplayAlertAsync("Error", "Failed to load sites.", "OK");
//				}
//			}
//			catch (Exception ex)
//			{
//				await Shell.Current.DisplayAlertAsync("Error", $"An error occurred: {ex.Message}", "OK");
//			}
//			finally
//			{
//				IsLoading = false;
//			}
//		}



//		[RelayCommand]
//		public void LoadMoreSites()
//		{
//			if (isFetchingMore || allSites.Count == 0) return;

//			isFetchingMore = true;

//			var newItems = allSites.Skip(currentChunkIndex * ChunkSize).Take(ChunkSize).ToList();
//			if (newItems.Count == 0)
//			{
//				isFetchingMore = false;
//				return; // No more items to load
//			}

//			currentChunkIndex++;

//			MainThread.BeginInvokeOnMainThread(() =>
//			{
//				foreach (var item in newItems)
//				{
//					FilteredSites.Add(item);
//				}
//			});

//			isFetchingMore = false;
//		}



//		private async void LoadSites()
//		{
//			IsLoading = true;
//			try
//			{
//				allSites.Clear();
//				var response = await _siteService.GetSitesAsync();

//				if (response != null && response.Success && response.Data != null)
//				{
//					allSites.AddRange(response.Data);
//				}
//				else
//				{
//					await Shell.Current.DisplayAlertAsync("Error", "Failed to load sites.", "OK");
//				}

//				FilteredSites.Clear();
//				foreach (var site in allSites)
//				{
//					FilteredSites.Add(site);
//				}
//			}
//			catch (Exception ex)
//			{
//				await Shell.Current.DisplayAlertAsync("Error", $"An error occurred: {ex.Message}", "OK");
//			}
//			finally
//			{
//				IsLoading = false;
//			}
//		}

//		[RelayCommand]
//		public async Task SelectSite(SiteModel selectedSite)
//		{
//			if (selectedSite == null)
//			{
//				Console.WriteLine("❌ SelectSiteCommand: No site selected.");
//				return;
//			}

//			var sessionService = MauiProgram.Services.GetRequiredService<SessionService>();


//			// Log old session values
//			Console.WriteLine($"🔹 Old SiteId: {sessionService.Get<int>("SiteId")}");
//			Console.WriteLine($"🔹 Old SiteName: {sessionService.Get<string>("SiteName")}");
//			Console.WriteLine($"🔹 Old GpsLocation: {sessionService.Get<string>("GpsLocation")}");


//			// Before navigating to AttendancePage

//			sessionService.Set("SiteId", selectedSite.SiteId);
//			sessionService.Set("SiteName", selectedSite.SiteName);
//			sessionService.Set("GpsLocation", selectedSite.GpsLocation);


//			// Log new session values
//			Console.WriteLine($"✅ New SiteId: {sessionService.Get<int>("SiteId")}");
//			Console.WriteLine($"✅ New SiteName: {sessionService.Get<string>("SiteName")}");
//			Console.WriteLine($"✅ New GpsLocation: {sessionService.Get<string>("GpsLocation")}");


//			await Shell.Current.GoToAsync("//DashboardPage");
//			await Shell.Current.GoToAsync(nameof(AttendancePage));

//			//await Shell.Current.GoToAsync(nameof(AttendancePage), navigationParameters);
//		}



//		partial void OnSearchTextChanged(string value)
//		{
//			if (string.IsNullOrWhiteSpace(value))
//			{
//				FilteredSites.Clear();
//				foreach (var site in allSites)
//				{
//					FilteredSites.Add(site);
//				}
//			}
//			else
//			{
//				var filteredList = allSites
//					.Where(s => s.SiteName.ToLower().Contains(value.ToLower()))
//					.ToList();

//				FilteredSites.Clear();
//				foreach (var site in filteredList)
//				{
//					FilteredSites.Add(site);
//				}
//			}
//		}
//	}
//}


using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using tutorial_demo_2.Models;
using tutorial_demo_2.Pages;
using tutorial_demo_2.Services;

namespace tutorial_demo_2.ViewModels
{
    public partial class SiteListViewModel : ObservableObject
    {
        private readonly ISiteService _siteService;
        private readonly IAppSecureStorage _secureStorage;

        private List<SiteModel> allSites = new();
        private List<SiteModel> currentResults = new();

        private int currentIndex;
        private const int ChunkSize = 20;
        private bool isFetchingMore;

        public ObservableCollection<SiteModel> FilteredSites { get; } = new();

        [ObservableProperty]
        private string searchText = string.Empty;

        [ObservableProperty]
        private bool isLoading;

        public SiteListViewModel(
            ISiteService siteService,
            IAppSecureStorage secureStorage)
        {
            _siteService = siteService
                ?? throw new ArgumentNullException(nameof(siteService));

            _secureStorage = secureStorage
                ?? throw new ArgumentNullException(nameof(secureStorage));

            _ = LoadSitesAsync();
        }

        private async Task LoadSitesAsync()
        {
            if (IsLoading)
                return;

            IsLoading = true;

            try
            {
                var response = await _siteService.GetSitesAsync();

                if (response?.Success == true && response.Data != null)
                {
                    allSites = response.Data
                        .OrderByDescending(s => s.SiteId)
                        .ToList();

                    ApplySearchAndReset();
                }
                else
                {
                    await Shell.Current.DisplayAlertAsync(
                        "Error",
                        "Failed to load sites.",
                        "OK");
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlertAsync(
                    "Error",
                    $"Unable to load sites: {ex.Message}",
                    "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ApplySearchAndReset()
        {
            var search = SearchText?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(search))
            {
                currentResults = allSites.ToList();
            }
            else
            {
                currentResults = allSites
                    .Where(s =>
                        (s.SiteName ?? string.Empty)
                        .Contains(search, StringComparison.OrdinalIgnoreCase)
                        ||
                        s.SiteId.ToString().Contains(search)
                        ||
                        (s.SiteLocation ?? string.Empty)
                        .Contains(search, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            currentIndex = 0;
            isFetchingMore = false;

            FilteredSites.Clear();

            LoadMoreSites();
        }

        [RelayCommand]
        public void LoadMoreSites()
        {
            if (isFetchingMore || currentIndex >= currentResults.Count)
                return;

            isFetchingMore = true;

            try
            {
                var newItems = currentResults
                    .Skip(currentIndex)
                    .Take(ChunkSize)
                    .ToList();

                foreach (var item in newItems)
                {
                    item.SerialNumber = FilteredSites.Count + 1;
                    FilteredSites.Add(item);
                }

                currentIndex += newItems.Count;
            }
            finally
            {
                isFetchingMore = false;
            }
        }

        partial void OnSearchTextChanged(string value)
        {
            ApplySearchAndReset();
        }

       



        [RelayCommand]
        public async Task SelectSite(SiteModel selectedSite)
        {
            if (selectedSite == null)
            {
                await Shell.Current.DisplayAlertAsync(
                    "Error",
                    "Please select a site.",
                    "OK");

                return;
            }

            try
            {
                var sessionService =
                    MauiProgram.Services.GetRequiredService<SessionService>();

                sessionService.Set("SiteId", selectedSite.SiteId);
                sessionService.Set("SiteName", selectedSite.SiteName);
                sessionService.Set("GpsLocation", selectedSite.GpsLocation);

                await Shell.Current.GoToAsync(
                    "AttendanceEntry",
                    true);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);

                await Shell.Current.DisplayAlertAsync(
                    "Navigation Error",
                    ex.Message,
                    "OK");
            }
        }



    }
}
