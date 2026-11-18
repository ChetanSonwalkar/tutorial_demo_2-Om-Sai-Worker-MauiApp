//using tutorial_demo_2.ViewModels;

//namespace tutorial_demo_2.Pages;

//public partial class AttendanceHistoryPage : ContentPage
//{
//	public AttendanceHistoryPage(AttendanceHistoryViewModel vm)
//	{
//		InitializeComponent();
//		BindingContext = vm;
//	}

//	protected override async void OnAppearing()
//	{
//		base.OnAppearing();
//		await (BindingContext as AttendanceHistoryViewModel)?.LoadAttendance();
//	}
//}



using tutorial_demo_2.ViewModels;

namespace tutorial_demo_2.Pages;

public partial class AttendanceHistoryPage : ContentPage
{
    private readonly AttendanceHistoryViewModel _viewModel;
    private bool _hasLoaded;

    public AttendanceHistoryPage(AttendanceHistoryViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_hasLoaded)
            return;

        _hasLoaded = true;

        await _viewModel.LoadAttendanceAsync();
    }
}
