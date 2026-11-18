using tutorial_demo_2.Models;

namespace tutorial_demo_2.Pages;

public partial class UserList : ContentPage
{
	public UserList()
	{
		InitializeComponent();

		var item = new List<UserModel>()
		{
			new UserModel{Username = "Rajesj", Password = "1234", Email = "0zK4F@example.com" },
			new UserModel{Username = "Rajesj1", Password = "1234", Email = "0zK4F@example.com" },
			new UserModel{Username = "Rajesj2", Password = "1234", Email = "0zK4F@example.com" },
			new UserModel{Username = "Rajesj3", Password = "1234", Email = "0zK4F@example.com" },
			new UserModel{Username = "Rajesj4", Password = "1234", Email = "0zK4F@example.com" },
			new UserModel{Username = "Rajesj5", Password = "1234", Email = "0zK4F@example.com" },
			new UserModel{Username = "Rajesj6", Password = "1234", Email = "0zK4F@example.com" },
			new UserModel{Username = "Rajesj7", Password = "1234", Email = "0zK4F@example.com" },
			new UserModel{Username = "Rajesj8", Password = "1234", Email = "0zK4F@example.com" },
			new UserModel{Username = "Rajesj9", Password = "1234", Email = "0zK4F@example.com" },
			new UserModel{Username = "Rajesj10", Password = "1234", Email = "0zK4F@example.com" }

		};

		listView.ItemsSource = item;
	}
}