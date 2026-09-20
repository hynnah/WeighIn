using WeighIn.Services;

namespace WeighIn;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();
		Loaded += OnShellLoaded;
	}

	private async void OnShellLoaded(object? sender, EventArgs e)
	{
		Loaded -= OnShellLoaded;

		var database = new AppDatabase();
		var profile = await database.GetProfileAsync();

		if (Application.Current is not null)
		{
			Application.Current.UserAppTheme = profile.PreferredTheme switch
			{
				"Dark" => AppTheme.Dark,
				"Light" => AppTheme.Light,
				_ => AppTheme.Unspecified
			};
		}

		if (!profile.HasOnboarded)
			await GoToAsync("//onboard");
		else if (profile.LockEnabled)
			await GoToAsync("//lock");
		else
			await GoToAsync("//main/home");
	}
}
