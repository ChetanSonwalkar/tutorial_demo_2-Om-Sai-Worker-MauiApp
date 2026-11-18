
using tutorial_demo_2.Services;
using tutorial_demo_2.ViewModels;

namespace tutorial_demo_2.Pages;

public partial class SiteListPage : ContentPage
{
    private readonly SiteListViewModel _viewModel;

    public SiteListPage(
        SiteListViewModel vm,
        ISiteService siteService,
        IAppSecureStorage secureStorage)
    {
        InitializeComponent();

        _viewModel = vm;
        BindingContext = _viewModel;
    }
}


//using Microsoft.Maui.Controls;
//using Microsoft.Maui.Storage;
//using tutorial_demo_2.Models;
//using tutorial_demo_2.Services;
//using tutorial_demo_2.ViewModels;

//namespace tutorial_demo_2.Pages;

//public partial class SiteListPage : ContentPage
//{

//	private readonly ISiteService _siteService;
//	private readonly IAppSecureStorage _secureStorage;

//	public SiteListPage(SiteListViewModel vm, ISiteService siteService, IAppSecureStorage secureStorage)
//	{
//		InitializeComponent();
//		_siteService = siteService;
//		_secureStorage = secureStorage;

//		BindingContext = new SiteListViewModel(_siteService, _secureStorage);
//	}

//}