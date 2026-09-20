using Microsoft.Extensions.DependencyInjection;
using WeighIn.Services;

namespace WeighIn;

public partial class App : Application
{
	private readonly AppDatabase database = new();
	private bool wentToBackground;

	public App()
	{
		InitializeComponent();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(new AppShell());
	}

	protected override void OnSleep() => wentToBackground = true;

	protected override async void OnResume()
	{
		if (!wentToBackground)
			return;

		wentToBackground = false;

		var profile = await database.GetProfileAsync();
		if (!profile.LockEnabled || Shell.Current is null)
			return;

		var currentRoute = Shell.Current.CurrentState.Location.OriginalString;
		if (!currentRoute.Contains("lock", StringComparison.OrdinalIgnoreCase) &&
			!currentRoute.Contains("onboard", StringComparison.OrdinalIgnoreCase))
		{
			await Shell.Current.GoToAsync("//lock");
		}
	}
}