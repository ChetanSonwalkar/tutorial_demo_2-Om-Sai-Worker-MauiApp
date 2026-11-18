using tutorial_demo_2.ViewModels;

namespace tutorial_demo_2.Pages;

public partial class DashboardPage : ContentPage
{
	private readonly DashboardViewModel _viewModel;
	public DashboardPage(DashboardViewModel vm)
	{
		InitializeComponent();
		_viewModel = vm;
		BindingContext = _viewModel;

	}

	protected override void OnAppearing()
	{
		base.OnAppearing();
		_viewModel.LoadUserDetailsCommand.Execute(null);
	}


}