//using System;
//using System.Collections.ObjectModel;
//using System.Linq;
//using System.Net.Http;
//using System.Threading.Tasks;
//using CommunityToolkit.Mvvm.ComponentModel;
//using CommunityToolkit.Mvvm.Input;
//using tutorial_demo_2.Models;
//using tutorial_demo_2.Services;

//namespace tutorial_demo_2.ViewModels
//{
//	public partial class AttendanceHistoryViewModel : ObservableObject
//	{
//		private readonly IAppStorageService _storageService;
//		private readonly IAttendanceService _attendanceService;

//		[ObservableProperty] private bool _isBusy;
//		[ObservableProperty] private ObservableCollection<AttendanceRecordList> _attendanceRecords = new();

//		public AttendanceHistoryViewModel(IAppStorageService storageService, IAttendanceService attendanceService)
//		{
//			_storageService = storageService;
//			_attendanceService = attendanceService;

//			_ = LoadAttendance(); // Fire and forget
//		}

//		[RelayCommand]
//		public async Task LoadAttendance()
//		{
//			if (IsBusy) return;

//			IsBusy = true;
//			try
//			{
//				var user = await _storageService.GetUserAsync();
//				if (user?.WorkerId == null)
//				{
//					await ShowAlert("Error", "User data is invalid!");
//					return;
//				}

//				var response = await _attendanceService.GetAllAttendanceAsync(user.WorkerId);
//				if (response.Success && response.Data?.Any() == true)
//				{
//					AttendanceRecords = new ObservableCollection<AttendanceRecordList>(
//						response.Data.Select(a => new AttendanceRecordList
//						{
//							CreatedAt = a.CreatedAt.ToString("dd MMM yy"),
//							InTime = a.InTime?.ToString("hh:mm tt") ?? "N/A",
//							OutTime = a.OutTime?.ToString("hh:mm tt") ?? "N/A",
//							WorkingHrs = FormatWorkingHours(a.WorkingHrs)
//						}));
//				}
//				else
//				{
//					await ShowAlert("Error", "Attendance record not found!");
//				}
//			}
//			catch (Exception ex)
//			{
//				await ShowAlert("Error", $"Something went wrong: {ex.Message}");
//			}
//			finally
//			{
//				IsBusy = false;
//			}
//		}

//		private static string FormatWorkingHours(int? minutes)
//			=> minutes > 0 ? $"{minutes / 60}:{minutes % 60:D2}" : "0:00";

//		private static Task ShowAlert(string title, string message)
//			=> Shell.Current.DisplayAlertAsync(title, message, "OK");
//	}
//}




using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using tutorial_demo_2.Models;
using tutorial_demo_2.Services;

namespace tutorial_demo_2.ViewModels;

public class AttendanceHistoryViewModel : INotifyPropertyChanged
{
    private readonly IAppStorageService _storageService;
    private readonly IAttendanceService _attendanceService;

    private List<AttendanceRecord> _allAttendanceRecords = new();

    private Dictionary<DateTime, List<AttendanceRecord>> _attendanceByDate = new();

    private DateTime _currentMonth =
        new(DateTime.Today.Year, DateTime.Today.Month, 1);

    private DateTime _selectedDate = DateTime.Today;
    private bool _isBusy;
    private bool _hasLoaded;
    private string _monthTitle = string.Empty;
    private string _selectedDateText = string.Empty;
    private string _selectedInTime = "N/A";
    private string _selectedOutTime = "N/A";
    private string _selectedWorkingHours = "0:00";
    private string _selectedStatusText = "Attendance Not Submitted";
    private string _errorMessage = string.Empty;

    public ObservableCollection<CalendarDayItem> CalendarDays { get; } = new();

    public DateTime SelectedDate
    {
        get => _selectedDate;
        set
        {
            DateTime newDate = value.Date;

            if (newDate > DateTime.Today)
                newDate = DateTime.Today;

            if (_selectedDate == newDate)
                return;

            _selectedDate = newDate;
            OnPropertyChanged();

            _currentMonth = new DateTime(
                newDate.Year,
                newDate.Month,
                1);

            BuildCalendar();
            UpdateSelectedDateDetails();
        }
    }

    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }

    public string MonthTitle
    {
        get => _monthTitle;
        set => SetProperty(ref _monthTitle, value);
    }

    public string SelectedDateText
    {
        get => _selectedDateText;
        set => SetProperty(ref _selectedDateText, value);
    }

    public string SelectedInTime
    {
        get => _selectedInTime;
        set => SetProperty(ref _selectedInTime, value);
    }

    public string SelectedOutTime
    {
        get => _selectedOutTime;
        set => SetProperty(ref _selectedOutTime, value);
    }

    public string SelectedWorkingHours
    {
        get => _selectedWorkingHours;
        set => SetProperty(ref _selectedWorkingHours, value);
    }

    public string SelectedStatusText
    {
        get => _selectedStatusText;
        set => SetProperty(ref _selectedStatusText, value);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public ICommand PreviousMonthCommand { get; }
    public ICommand NextMonthCommand { get; }
    public ICommand SelectDayCommand { get; }
    public ICommand LoadAttendanceCommand { get; }

    public AttendanceHistoryViewModel(
        IAppStorageService storageService,
        IAttendanceService attendanceService)
    {
        _storageService = storageService;
        _attendanceService = attendanceService;

        MonthTitle = _currentMonth.ToString("MMMM yyyy");
        SelectedDateText = _selectedDate.ToString("dddd, dd MMMM yyyy");

        PreviousMonthCommand = new Command(PreviousMonth);
        NextMonthCommand = new Command(NextMonth);
        SelectDayCommand = new Command<CalendarDayItem>(SelectDay);
        LoadAttendanceCommand = new Command(
            async () => await LoadAttendanceAsync(true));

        BuildCalendar();
        UpdateSelectedDateDetails();
    }

    public async Task LoadAttendanceAsync(bool forceRefresh = false)
    {
        if (IsBusy)
            return;

        if (_hasLoaded && !forceRefresh)
            return;

        try
        {
            IsBusy = true;
            ErrorMessage = string.Empty;

            var user = await _storageService.GetUserAsync();
            int workerId = user?.WorkerId ?? 0;

            if (workerId <= 0)
            {
                ErrorMessage = "User details not found. Please login again.";
                return;
            }

            var response =
                await _attendanceService.GetAllAttendanceAsync(workerId);

            if (response == null || !response.Success)
            {
                ErrorMessage = "Unable to load attendance records.";
                return;
            }

            _allAttendanceRecords = response.Data?
                .Where(x => x != null)
                .ToList() ?? new List<AttendanceRecord>();

            BuildAttendanceLookup();

            _hasLoaded = true;

            BuildCalendar();
            UpdateSelectedDateDetails();
        }
        catch (Exception ex)
        {
            ErrorMessage = "Attendance loading failed: " + ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void BuildAttendanceLookup()
    {
        _attendanceByDate = _allAttendanceRecords
            .Where(x => GetAttendanceDate(x).HasValue)
            .GroupBy(x => GetAttendanceDate(x)!.Value.Date)
            .ToDictionary(
                group => group.Key,
                group => group.OrderBy(x => x.InTime ?? DateTime.MaxValue).ToList());
    }

    private static DateTime? GetAttendanceDate(AttendanceRecord record)
    {
        if (record.InTime.HasValue)
            return record.InTime.Value.Date;

        if (record.OutTime.HasValue)
            return record.OutTime.Value.Date;

        if (record.CreatedAt != default)
            return record.CreatedAt.Date;

        return null;
    }

    private void BuildCalendar()
    {
        CalendarDays.Clear();

        MonthTitle = _currentMonth.ToString("MMMM yyyy");

        DateTime firstDay = new(
            _currentMonth.Year,
            _currentMonth.Month,
            1);

        int offset = ((int)firstDay.DayOfWeek + 6) % 7;
        int daysInMonth = DateTime.DaysInMonth(
            _currentMonth.Year,
            _currentMonth.Month);

        int totalCells = ((offset + daysInMonth + 6) / 7) * 7;

        for (int i = 0; i < totalCells; i++)
        {
            int day = i - offset + 1;

            if (day < 1 || day > daysInMonth)
            {
                CalendarDays.Add(new CalendarDayItem());
                continue;
            }

            DateTime date = new(
                _currentMonth.Year,
                _currentMonth.Month,
                day);

            bool hasAttendance = _attendanceByDate.ContainsKey(date.Date);
            bool isFuture = date.Date > DateTime.Today;
            bool isSelected = date.Date == _selectedDate.Date;

            string background;
            string textColor;

            if (hasAttendance)
            {
                background = "#16A34A";
                textColor = "#FFFFFF";
            }
            else if (isFuture)
            {
                background = "#E2E8F0";
                textColor = "#64748B";
            }
            else
            {
                background = "#FEE2E2";
                textColor = "#DC2626";
            }

            CalendarDays.Add(new CalendarDayItem
            {
                Date = date,
                DayNumber = day.ToString(),
                IsEnabled = !isFuture,
                IsSelected = isSelected,
                DayBackgroundColor = background,
                DayTextColor = textColor,
                DayBorderColor = isSelected ? "#3157D5" : "Transparent"
            });
        }
    }

    private void SelectDay(CalendarDayItem? item)
    {
        if (item?.Date == null || !item.IsEnabled)
            return;

        SelectedDate = item.Date.Value;
    }

    private void UpdateSelectedDateDetails()
    {
        SelectedDateText =
            _selectedDate.ToString("dddd, dd MMMM yyyy");

        if (!_attendanceByDate.TryGetValue(
            _selectedDate.Date,
            out List<AttendanceRecord>? records))
        {
            SelectedInTime = "N/A";
            SelectedOutTime = "N/A";
            SelectedWorkingHours = "0:00";

            SelectedStatusText = _selectedDate.Date > DateTime.Today
                ? "Future Date"
                : "Attendance Not Submitted";

            return;
        }

        DateTime? firstIn = records
            .Where(x => x.InTime.HasValue)
            .Select(x => x.InTime)
            .Min();

        DateTime? lastOut = records
            .Where(x => x.OutTime.HasValue)
            .Select(x => x.OutTime)
            .Max();

        SelectedInTime = firstIn?.ToString("hh:mm tt") ?? "N/A";
        SelectedOutTime = lastOut?.ToString("hh:mm tt") ?? "N/A";

        int totalMinutes = records.Sum(GetWorkingMinutes);

        SelectedWorkingHours = FormatWorkingHours(totalMinutes);
        SelectedStatusText = "Attendance Submitted";
    }

    private static int GetWorkingMinutes(AttendanceRecord record)
    {
        if (record.WorkingHrs.HasValue && record.WorkingHrs.Value > 0)
            return record.WorkingHrs.Value;

        if (record.InTime.HasValue && record.OutTime.HasValue)
        {
            TimeSpan duration = record.OutTime.Value - record.InTime.Value;

            if (duration.TotalMinutes < 0)
                duration = duration.Add(TimeSpan.FromDays(1));

            return Math.Max(0, (int)duration.TotalMinutes);
        }

        return 0;
    }

    private static string FormatWorkingHours(int minutes)
    {
        return $"{minutes / 60}:{minutes % 60:D2}";
    }

    private void PreviousMonth()
    {
        _currentMonth = _currentMonth.AddMonths(-1);

        _selectedDate = new DateTime(
            _currentMonth.Year,
            _currentMonth.Month,
            1);

        OnPropertyChanged(nameof(SelectedDate));

        BuildCalendar();
        UpdateSelectedDateDetails();
    }

    private void NextMonth()
    {
        DateTime nextMonth = _currentMonth.AddMonths(1);

        DateTime currentMonth = new(
            DateTime.Today.Year,
            DateTime.Today.Month,
            1);

        if (nextMonth > currentMonth)
            return;

        _currentMonth = nextMonth;

        _selectedDate = new DateTime(
            _currentMonth.Year,
            _currentMonth.Month,
            1);

        if (_selectedDate > DateTime.Today)
            _selectedDate = DateTime.Today;

        OnPropertyChanged(nameof(SelectedDate));

        BuildCalendar();
        UpdateSelectedDateDetails();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void SetProperty<T>(
        ref T field,
        T value,
        [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return;

        field = value;
        OnPropertyChanged(propertyName);
    }

    private void OnPropertyChanged(
        [CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(propertyName));
    }
}


