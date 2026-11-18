
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace tutorial_demo_2.Models;

public class CalendarDayItem : INotifyPropertyChanged
{
    private DateTime? _date;
    private string _dayNumber = string.Empty;
    private bool _isEnabled;
    private bool _isSelected;
    private string _dayBackgroundColor = "Transparent";
    private string _dayTextColor = "Transparent";
    private string _dayBorderColor = "Transparent";

    public DateTime? Date
    {
        get => _date;
        set => SetProperty(ref _date, value);
    }

    public string DayNumber
    {
        get => _dayNumber;
        set => SetProperty(ref _dayNumber, value);
    }

    public bool IsEnabled
    {
        get => _isEnabled;
        set => SetProperty(ref _isEnabled, value);
    }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public string DayBackgroundColor
    {
        get => _dayBackgroundColor;
        set => SetProperty(ref _dayBackgroundColor, value);
    }

    public string DayTextColor
    {
        get => _dayTextColor;
        set => SetProperty(ref _dayTextColor, value);
    }

    public string DayBorderColor
    {
        get => _dayBorderColor;
        set => SetProperty(ref _dayBorderColor, value);
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
        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(propertyName));
    }
}
