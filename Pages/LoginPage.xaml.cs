using tutorial_demo_2.ViewModels;

namespace tutorial_demo_2.Pages;

public partial class LoginPage : ContentPage
{
	public LoginPage(LoginViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}

}