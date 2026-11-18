
using tutorial_demo_2.ViewModels;

namespace tutorial_demo_2.Pages;

public partial class WorkerListPage : ContentPage
{
    private readonly WorkerListViewModel _viewModel;

    public WorkerListPage(WorkerListViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel
            ?? throw new ArgumentNullException(nameof(viewModel));

        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            await _viewModel.InitializeAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"WorkerListPage Error: {ex}");

            await DisplayAlertAsync(
                "Worker List Error",
                ex.Message,
                "OK");
        }
    }
}



//using tutorial_demo_2.ViewModels;

//namespace tutorial_demo_2.Pages;

//public partial class WorkerListPage : ContentPage
//{
//	public WorkerListPage(WorkerListViewModel vm)
//	{
//		InitializeComponent();
//		BindingContext = vm;
//	}
//}