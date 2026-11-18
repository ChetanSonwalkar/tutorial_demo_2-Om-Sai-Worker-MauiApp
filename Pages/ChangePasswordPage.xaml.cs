using tutorial_demo_2.ViewModels;

namespace tutorial_demo_2.Pages
{
	public partial class ChangePasswordPage : ContentPage
	{
		public ChangePasswordPage(ChangePasswordViewModel viewModel)
		{
			InitializeComponent();
			BindingContext = viewModel;
		}
	}
}
