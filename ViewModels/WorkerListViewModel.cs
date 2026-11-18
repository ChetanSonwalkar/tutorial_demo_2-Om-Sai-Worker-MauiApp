
using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using tutorial_demo_2.Models;
using tutorial_demo_2.Services;

namespace tutorial_demo_2.ViewModels;

public partial class WorkerListViewModel : ObservableObject
{
    private readonly AttendanceViewModel _attendanceViewModel;
    private readonly IWorkerService _workerService;
    private readonly IAppSecureStorage _secureStorage;

    private readonly List<Workers> _allWorkers = new();
    private bool _isLoaded;
    private readonly SemaphoreSlim _loadLock = new(1, 1);

    public ObservableCollection<Workers> FilteredWorkers { get; } = new();

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private Workers? selectedWorker;

    public WorkerListViewModel(
        AttendanceViewModel attendanceViewModel,
        IWorkerService workerService,
        IAppSecureStorage secureStorage)
    {
        _attendanceViewModel = attendanceViewModel
            ?? throw new ArgumentNullException(nameof(attendanceViewModel));

        _workerService = workerService
            ?? throw new ArgumentNullException(nameof(workerService));

        _secureStorage = secureStorage
            ?? throw new ArgumentNullException(nameof(secureStorage));
    }

    public async Task InitializeAsync()
    {
        if (_isLoaded)
            return;

        await _loadLock.WaitAsync();

        try
        {
            if (_isLoaded)
                return;

            await LoadWorkersAsync();
        }
        finally
        {
            _loadLock.Release();
        }
    }

    private async Task LoadWorkersAsync()
    {
        try
        {
            IsLoading = true;

            _allWorkers.Clear();
            FilteredWorkers.Clear();

            var userDetails = await _secureStorage.GetUserDetailsAsync();

            if (userDetails == null)
            {
                await ShowAlertAsync(
                    "Login Required",
                    "User details not found. Please login again.");

                await Shell.Current.GoToAsync("//LoginPage", true);
                return;
            }

            int siteId = userDetails.SiteId;

            if (siteId <= 0)
            {
                await ShowAlertAsync(
                    "Site Missing",
                    "Site ID is not available for the logged-in user.");

                return;
            }

            System.Diagnostics.Debug.WriteLine(
                $"Loading workers for SiteId: {siteId}");

            var response = await _workerService.WorkerListAsync(siteId);

            if (response == null)
            {
                await ShowAlertAsync(
                    "API Error",
                    "The worker API returned an empty response.");

                return;
            }

            if (!response.Success || response.Data == null)
            {
                await ShowAlertAsync(
                    "Worker List",
                    "Unable to load workers. Please check the API response.");

                return;
            }

            foreach (var item in response.Data)
            {
                if (item == null || !item.WorkerId.HasValue ||
                    item.WorkerId.Value <= 0)
                {
                    continue;
                }

                string imageUrl = !string.IsNullOrWhiteSpace(item.ProfileImage)
                    ? $"{ApiUrls.ImageUrl.TrimEnd('/')}/{item.ProfileImage.TrimStart('/')}"
                    : "man.png";

                _allWorkers.Add(new Workers
                {
                    WorkerId = item.WorkerId.Value,
                    WorkmanId = item.WorkmanId ?? string.Empty,
                    Name = item.Name?.Trim() ?? string.Empty,
                    ProfileImage = imageUrl
                });
            }

            ApplySearchFilter();

            _isLoaded = true;

            System.Diagnostics.Debug.WriteLine(
                $"Workers loaded: {_allWorkers.Count}");

            if (_allWorkers.Count == 0)
            {
                await ShowAlertAsync(
                    "Worker List",
                    "No workers found for this site.");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"LoadWorkersAsync Error: {ex}");

            await ShowAlertAsync(
                "Worker Loading Error",
                ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SelectWorkerAsync(Workers? worker)
    {
        if (worker == null)
            return;

        if (worker.WorkerId <= 0 ||
            string.IsNullOrWhiteSpace(worker.WorkmanId))
        {
            await ShowAlertAsync(
                "Invalid Worker",
                "Worker details are incomplete.");

            return;
        }

        try
        {
            IsLoading = true;
            SelectedWorker = worker;

            System.Diagnostics.Debug.WriteLine(
                $"Selected WorkerId: {worker.WorkerId}");

            System.Diagnostics.Debug.WriteLine(
                $"Selected WorkmanId: {worker.WorkmanId}");

            await _attendanceViewModel.ExternalNavigateToSelfiePage(
                worker.WorkmanId,
                worker.WorkerId);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"SelectWorkerAsync Error: {ex}");

            await ShowAlertAsync(
                "Attendance Error",
                ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnSearchTextChanged(string value)
    {
        ApplySearchFilter();
    }

    private void ApplySearchFilter()
    {
        var search = SearchText?.Trim() ?? string.Empty;

        var filtered = string.IsNullOrWhiteSpace(search)
            ? _allWorkers
            : _allWorkers
                .Where(x =>
                    (x.Name ?? string.Empty)
                    .Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    (x.WorkmanId ?? string.Empty)
                    .Contains(search, StringComparison.OrdinalIgnoreCase))
                .ToList();

        FilteredWorkers.Clear();

        foreach (var worker in filtered)
        {
            FilteredWorkers.Add(worker);
        }
    }

    private static Task ShowAlertAsync(string title, string message)
    {
        return MainThread.InvokeOnMainThreadAsync(
            () => Shell.Current.DisplayAlertAsync(title, message, "OK"));
    }
}



//using System.Collections.ObjectModel;
//using CommunityToolkit.Mvvm.ComponentModel;
//using CommunityToolkit.Mvvm.Input;
//using tutorial_demo_2.Models;
//using CommunityToolkit.Mvvm.Collections;
//using tutorial_demo_2.Services;
//using System.Windows.Input;
//using Microsoft.Maui.Storage;

//namespace tutorial_demo_2.ViewModels
//{

//	public partial class WorkerListViewModel :  ObservableObject
//	{
//		// Original full worker list
//		private List<Workers> allWorkers = new List<Workers>();

//		private readonly AttendanceViewModel _attendanceViewModel;
//		private readonly IWorkerService _workerService;
//		private readonly IAppSecureStorage _secureStorage;
//		// Filtered worker list
//		public ObservableCollection<Workers> FilteredWorkers { get; set; } = new ObservableCollection<Workers>();

//		// Search text
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

//		private Workers _selectedWorker;
//		public Workers SelectedWorker
//		{
//			get => _selectedWorker;
//			set
//			{
//				_selectedWorker = value;
//				OnPropertyChanged();
//			}
//		}




//		public WorkerListViewModel(AttendanceViewModel attendanceViewModel, IWorkerService workerService, IAppSecureStorage secureStorage)
//		{
//			_secureStorage = secureStorage ?? throw new ArgumentNullException(nameof(secureStorage));
//			_workerService = workerService ?? throw new ArgumentNullException(nameof(workerService));
//			_attendanceViewModel = attendanceViewModel;

//			LoadWorkers();
//			//_secureStorage = appSecureStorage;
//		}

//		private async void LoadWorkers()
//		{
//			IsLoading = true;
//			try
//			{
//				// Clear existing workers
//				allWorkers.Clear();

//				var _userDetails = await _secureStorage.GetUserDetailsAsync();
//				if (_userDetails == null)
//				{
//					await MainThread.InvokeOnMainThreadAsync(async () =>
//					{
//						await Shell.Current.DisplayAlertAsync("Error", "User details not found. Please login again.", "OK");
//					});
//					await Shell.Current.GoToAsync("//LoginPage", true);
//					return;
//				}

//				// Call the API to fetch worker list (replace `siteId` with actual siteId)
//				int siteId = _userDetails.SiteId; // Change this as needed
//				var response = await _workerService.WorkerListAsync(siteId);

//				// Check if API response is successful
//				if (response != null && response.Success && response.Data != null)
//				{
//					// Convert WorkerListData to Workers model and add them to the list
//					foreach (var worker in response.Data)
//					{
//						allWorkers.Add(new Workers
//						{
//							WorkerId = worker.WorkerId ?? 0,
//							WorkmanId = worker.WorkmanId,
//							Name = worker.Name,
//							//ProfileImage = worker.ProfileImage
//							ProfileImage = !string.IsNullOrEmpty(worker.ProfileImage)
//											? $"{ApiUrls.ImageUrl}/{worker.ProfileImage}"
//											: "man.png"
//						});
//					}
//				}
//				else
//				{
//					await Shell.Current.DisplayAlertAsync("Error", "Failed to load workers.", "OK");
//				}

//				// Ensure the initial list is displayed
//				FilteredWorkers.Clear();
//				foreach (var worker in allWorkers)
//				{
//					FilteredWorkers.Add(worker);
//				}

//				// Update the UI with the new worker list
//				//FilteredWorkers = new ObservableCollection<Workers>(allWorkers);
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
//		private async Task SelectWorker(Workers worker)
//		{
//			IsLoading = true;
//			SelectedWorker = worker;

//			await _attendanceViewModel.ExternalNavigateToSelfiePage(worker.WorkmanId, worker.WorkerId);
//			IsLoading = false;

//		}



//		partial void OnSearchTextChanged(string value)
//		{
//			if (string.IsNullOrWhiteSpace(value))
//			{
//				// Show all workers if search text is empty
//				FilteredWorkers.Clear();
//				foreach (var worker in allWorkers)
//				{
//					FilteredWorkers.Add(worker);
//				}
//			}
//			else
//			{
//				// Filter workers based on search text
//				var filteredList = allWorkers
//					.Where(w => w.Name.ToLower().Contains(value.ToLower()))
//					.ToList();

//				FilteredWorkers.Clear();
//				foreach (var worker in filteredList)
//				{
//					FilteredWorkers.Add(worker);
//				}
//			}
//		}




//	}
//}
