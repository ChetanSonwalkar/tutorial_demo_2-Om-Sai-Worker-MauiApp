//using System.Collections.ObjectModel;
//using System.ComponentModel;
//using tutorial_demo_2.Models;
//using tutorial_demo_2.Services;
//using System.Windows.Input;


//namespace tutorial_demo_2.Pages;

//public partial class LeavePage : ContentPage, INotifyPropertyChanged
//{

//	private readonly ILeaveRequestService _leaveRequestService;
//	private List<LeaveTypeModel> _leaveTypes;
//	private int _workerId;
//	private bool _isFormVisible;
//	private ObservableCollection<LeaveRequestModel> _leaves;
//	private bool _isLoading;

//	public ObservableCollection<LeaveRequestModel> Leaves
//	{
//		get => _leaves;
//		set
//		{
//			_leaves = value;
//			OnPropertyChanged(nameof(Leaves));
//		}
//	}

//	public bool IsFormVisible
//	{
//		get => _isFormVisible;
//		set
//		{
//			_isFormVisible = value;
//			OnPropertyChanged(nameof(IsFormVisible));
//		}
//	}

//	public bool IsLoading
//	{
//		get => _isLoading;
//		set
//		{
//			_isLoading = value;
//			OnPropertyChanged(nameof(IsLoading));
//		}
//	}

//	public ICommand ToggleFormCommand { get; }
//	public ICommand SubmitCommand { get; }
//	public ICommand DeleteCommand { get; }


//	public LeavePage(ILeaveRequestService leaveRequestService)
//	{
//		InitializeComponent();

//		fromDatePicker.MinimumDate = DateTime.Today;
//		toDatePicker.MinimumDate = DateTime.Today;

//		_leaveRequestService = leaveRequestService;
//		LoadWorkerIdFromSecureStorage();

//		Leaves = new ObservableCollection<LeaveRequestModel>();
//		IsFormVisible = false;
//		IsLoading = false;
//		ToggleFormCommand = new Command(ToggleForm);
//		SubmitCommand = new Command(async () => await OnSubmitClicked());
//		DeleteCommand = new Command<int>(async (leaveRequestId) => await OnDeleteClicked(leaveRequestId));

//		BindingContext = this;

//	}

//	private void ToggleForm()
//	{
//		IsFormVisible = !IsFormVisible;
//	}

//	private async void LoadWorkerIdFromSecureStorage()
//	{
//		var secureStorage = new AppSecureStorage();
//		var userDetails = await secureStorage.GetUserDetailsAsync();
//		if (userDetails != null)
//		{
//			_workerId = userDetails.WorkerId;
//		}
//	}

//	protected override async void OnAppearing()
//	{
//		base.OnAppearing();
//		await LoadLeaveTypes();
//		await LoadWorkerLeaves();
//	}

//	private async Task LoadLeaveTypes()
//	{
//		try
//		{
//			_leaveTypes = await _leaveRequestService.GetLeaveTypesAsync();
//			leaveTypePicker.ItemsSource = _leaveTypes.Select(lt => lt.LeaveTypeName).ToList();
//		}
//		catch (Exception ex)
//		{
//			await DisplayAlertAsync("Error", "Failed to load leave types", "OK");
//		}
//	}

//	private async Task LoadWorkerLeaves()
//	{
//		try
//		{
//			IsLoading = true;
//			var leaves = await _leaveRequestService.GetWorkerLeavesAsync(_workerId);
//			Leaves.Clear();
//			foreach (var leave in leaves)
//			{
//				Leaves.Add(leave);
//			}
//		}
//		catch (Exception ex)
//		{
//			await DisplayAlertAsync("Error", "Failed to load leave requests", "OK");
//		}
//		finally
//		{
//			IsLoading = false;
//		}
//	}

//	private async void BackButton_Clicked(object sender, EventArgs e)
//	{
//		await Shell.Current.GoToAsync("//Dashboard");
//	}

//	private async Task OnSubmitClicked()
//	{
//		if (string.IsNullOrEmpty(leaveTypePicker.SelectedItem?.ToString()))
//		{
//			await DisplayAlertAsync("Error", "Please select a leave type", "OK");
//			return;
//		}

//		if (fromDatePicker.Date > toDatePicker.Date)
//		{
//			await DisplayAlertAsync("Error", "From date cannot be after to date", "OK");
//			return;
//		}

//		if (string.IsNullOrWhiteSpace(reasonEditor.Text))
//		{
//			await DisplayAlertAsync("Error", "Please enter a reason for leave", "OK");
//			return;
//		}

//		try
//		{
//			var selectedLeaveType = _leaveTypes.FirstOrDefault(lt => lt.LeaveTypeName == leaveTypePicker.SelectedItem.ToString());
//			if (selectedLeaveType == null)
//			{
//				await DisplayAlertAsync("Error", "Invalid leave type selected", "OK");
//				return;
//			}

//			var leaveRequest = new LeaveRequestCreateModel
//			{
//				WorkerId = _workerId,
//				LeaveTypeId = selectedLeaveType.LeaveTypeId,
//                StartDate = $"{fromDatePicker.Date:yyyy-MM-dd}",
//                EndDate = $"{toDatePicker.Date:yyyy-MM-dd}",

//                Reason = reasonEditor.Text
//			};

//			var success = await _leaveRequestService.CreateLeaveRequestAsync(leaveRequest);
//			if (success)
//			{
//				await DisplayAlertAsync("Success", "Leave request submitted successfully", "OK");
//				// Reset form
//				leaveTypePicker.SelectedItem = null;
//				fromDatePicker.Date = DateTime.Today;
//				toDatePicker.Date = DateTime.Today;
//				reasonEditor.Text = string.Empty;
//				IsFormVisible = false;  // Hide form after successful submission
//				await LoadWorkerLeaves();  // Refresh the list
//			}
//			else
//			{
//				await DisplayAlertAsync("Error", "Failed to submit leave request", "OK");
//			}
//		}
//		catch (Exception ex)
//		{
//			await DisplayAlertAsync("Error", "An error occurred while submitting the request", "OK");
//		}
//	}

//	private async Task OnDeleteClicked(int leaveRequestId)
//	{
//		try
//		{
//			bool answer = await DisplayAlertAsync("Confirm Delete", "Are you sure you want to delete this leave request?", "Yes", "No");
//			if (!answer) return;

//			IsLoading = true;
//			bool success = await _leaveRequestService.DeleteLeaveRequestAsync(leaveRequestId);
//			if (success)
//			{
//				var itemToRemove = Leaves.FirstOrDefault(l => l.LeaveRequestId == leaveRequestId);
//				if (itemToRemove != null)
//				{
//					Leaves.Remove(itemToRemove);
//				}
//				await DisplayAlertAsync("Success", "Leave request deleted successfully!", "OK");
//			}
//			else
//			{
//				await DisplayAlertAsync("Error", "Failed to delete leave request. Please try again.", "OK");
//			}
//		}
//		catch (Exception ex)
//		{
//			await DisplayAlertAsync("Error", "An error occurred while deleting the leave request.", "OK");
//		}
//		finally
//		{
//			IsLoading = false;
//		}
//	}

//	public event PropertyChangedEventHandler PropertyChanged;

//	protected virtual void OnPropertyChanged(string propertyName)
//	{
//		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
//	}


//}



using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using tutorial_demo_2.Models;
using tutorial_demo_2.Services;

namespace tutorial_demo_2.Pages;

public partial class LeavePage : ContentPage, INotifyPropertyChanged
{
    private readonly ILeaveRequestService _leaveRequestService;
    private readonly AppSecureStorage _secureStorage;

    private List<LeaveTypeModel> _leaveTypes = new();
    private ObservableCollection<LeaveRequestModel> _leaves = new();

    private int _workerId;
    private bool _isFormVisible;
    private bool _isLoading;
    private bool _isInitialized;

    public ObservableCollection<LeaveRequestModel> Leaves
    {
        get => _leaves;
        set
        {
            _leaves = value;
            OnPropertyChanged(nameof(Leaves));
        }
    }

    public bool IsFormVisible
    {
        get => _isFormVisible;
        set
        {
            if (_isFormVisible == value)
                return;

            _isFormVisible = value;
            OnPropertyChanged(nameof(IsFormVisible));
        }
    }

    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            if (_isLoading == value)
                return;

            _isLoading = value;
            OnPropertyChanged(nameof(IsLoading));
        }
    }

    public ICommand ToggleFormCommand { get; }
    public ICommand SubmitCommand { get; }
    public ICommand DeleteCommand { get; }

    public LeavePage(ILeaveRequestService leaveRequestService)
    {
        InitializeComponent();

        _leaveRequestService = leaveRequestService
            ?? throw new ArgumentNullException(nameof(leaveRequestService));

        _secureStorage = new AppSecureStorage();

        fromDatePicker.Date = DateTime.Today;
        toDatePicker.Date = DateTime.Today;

        fromDatePicker.MinimumDate = DateTime.Today;
        toDatePicker.MinimumDate = DateTime.Today;

        ToggleFormCommand = new Command(ToggleForm);

        SubmitCommand = new Command(
            async () => await OnSubmitClicked());

        DeleteCommand = new Command<int>(
            async id => await OnDeleteClicked(id));

        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_isInitialized || IsLoading)
            return;

        await InitializePageAsync();
    }

    private async Task InitializePageAsync()
    {
        IsLoading = true;

        try
        {
            var userDetails = await _secureStorage.GetUserDetailsAsync();

            if (userDetails == null || userDetails.WorkerId <= 0)
            {
                await DisplayAlertAsync(
                    "Login Required",
                    "Worker details not found. Please login again.",
                    "OK");

                return;
            }

            _workerId = userDetails.WorkerId;

            await LoadLeaveTypes();
            await LoadWorkerLeaves();

            _isInitialized = true;
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(
                "Error",
                $"Unable to initialize leave page: {ex.Message}",
                "OK");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private static DateTime GetPickerDate(DatePicker picker)
    {
        return picker.Date ?? DateTime.Today;
    }

    private void ToggleForm()
    {
        IsFormVisible = !IsFormVisible;

        if (IsFormVisible)
        {
            ResetForm();
        }
    }

    private void ResetForm()
    {
        leaveTypePicker.SelectedItem = null;
        reasonEditor.Text = string.Empty;

        fromDatePicker.Date = DateTime.Today;
        toDatePicker.Date = DateTime.Today;

        fromDatePicker.MinimumDate = DateTime.Today;
        toDatePicker.MinimumDate = DateTime.Today;
    }

    private void OnCancelClicked(object sender, EventArgs e)
    {
        IsFormVisible = false;
        ResetForm();
    }

    private async Task LoadLeaveTypes()
    {
        try
        {
            _leaveTypes =
                await _leaveRequestService.GetLeaveTypesAsync()
                ?? new List<LeaveTypeModel>();

            leaveTypePicker.ItemsSource = _leaveTypes
                .Where(x => x.Status)
                .Select(x => x.LeaveTypeName)
                .ToList();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(
                "Error",
                $"Failed to load leave types: {ex.Message}",
                "OK");
        }
    }

    private async Task LoadWorkerLeaves()
    {
        if (_workerId <= 0)
            return;

        try
        {
            var leaves =
                await _leaveRequestService.GetWorkerLeavesAsync(_workerId);

            Leaves.Clear();

            if (leaves != null)
            {
                foreach (var leave in leaves
                    .OrderByDescending(x => x.CreatedAt)
                    .ThenByDescending(x => x.LeaveRequestId))
                {
                    Leaves.Add(leave);
                }
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(
                "Error",
                $"Failed to load leave requests: {ex.Message}",
                "OK");
        }
    }

    private void OnFromDateSelected(
    object sender,
    DateChangedEventArgs e)
    {
        if (!e.NewDate.HasValue)
            return;

        DateTime selectedFromDate = e.NewDate.Value.Date;

        toDatePicker.MinimumDate = selectedFromDate;

        DateTime? selectedToDate = toDatePicker.Date;

        if (selectedToDate.HasValue &&
            selectedToDate.Value.Date < selectedFromDate)
        {
            toDatePicker.Date = selectedFromDate;
        }
    }

    private async Task OnSubmitClicked()
    {
        if (IsLoading)
            return;

        if (_workerId <= 0)
        {
            await DisplayAlertAsync(
                "Error",
                "Worker ID not found. Please login again.",
                "OK");

            return;
        }

        if (leaveTypePicker.SelectedItem == null)
        {
            await DisplayAlertAsync(
                "Validation",
                "Please select a leave type.",
                "OK");

            return;
        }

        DateTime startDate = GetPickerDate(fromDatePicker);
        DateTime endDate = GetPickerDate(toDatePicker);

        if (startDate.Date < DateTime.Today)
        {
            await DisplayAlertAsync(
                "Validation",
                "From date cannot be in the past.",
                "OK");

            return;
        }

        if (startDate.Date > endDate.Date)
        {
            await DisplayAlertAsync(
                "Validation",
                "From date cannot be after to date.",
                "OK");

            return;
        }

        if (string.IsNullOrWhiteSpace(reasonEditor.Text))
        {
            await DisplayAlertAsync(
                "Validation",
                "Please enter a reason for leave.",
                "OK");

            return;
        }

        var selectedLeaveType = _leaveTypes.FirstOrDefault(
            x => x.Status &&
                 x.LeaveTypeName == leaveTypePicker.SelectedItem.ToString());

        if (selectedLeaveType == null)
        {
            await DisplayAlertAsync(
                "Error",
                "Invalid leave type selected.",
                "OK");

            return;
        }

        try
        {
            IsLoading = true;

            var leaveRequest = new LeaveRequestCreateModel
            {
                WorkerId = _workerId,
                LeaveTypeId = selectedLeaveType.LeaveTypeId,
                Reason = reasonEditor.Text.Trim()
            };

            leaveRequest.SetDates(startDate, endDate);

            bool success =
                await _leaveRequestService.CreateLeaveRequestAsync(leaveRequest);

            if (success)
            {
                await DisplayAlertAsync(
                    "Success",
                    "Leave request submitted successfully.",
                    "OK");

                ResetForm();
                IsFormVisible = false;

                await LoadWorkerLeaves();
            }
            else
            {
                await DisplayAlertAsync(
                    "Error",
                    "Failed to submit leave request. Please try again.",
                    "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(
                "Submission Error",
                ex.Message,
                "OK");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task OnDeleteClicked(int leaveRequestId)
    {
        if (leaveRequestId <= 0 || IsLoading)
            return;

        try
        {
            bool answer = await DisplayAlertAsync(
                "Confirm Delete",
                "Are you sure you want to delete this leave request?",
                "Yes",
                "No");

            if (!answer)
                return;

            IsLoading = true;

            bool success =
                await _leaveRequestService.DeleteLeaveRequestAsync(leaveRequestId);

            if (success)
            {
                var item = Leaves.FirstOrDefault(
                    x => x.LeaveRequestId == leaveRequestId);

                if (item != null)
                    Leaves.Remove(item);

                await DisplayAlertAsync(
                    "Success",
                    "Leave request deleted successfully.",
                    "OK");
            }
            else
            {
                await DisplayAlertAsync(
                    "Error",
                    "Failed to delete leave request.",
                    "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(
                "Delete Error",
                ex.Message,
                "OK");
        }
        finally
        {
            IsLoading = false;
        }
    }

    public new event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(propertyName));
    }
}

