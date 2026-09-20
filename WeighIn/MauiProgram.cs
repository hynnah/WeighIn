using Microsoft.Extensions.Logging;
using WeighIn.Services;

namespace WeighIn;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.Services.AddSingleton<AppDatabase>();

		builder
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

#if ANDROID
		// The Android TimePicker's own summary text doesn't render in this SDK version (a known
		// MAUI handler quirk), so pages overlay a Label on top of an invisible-text TimePicker.
		// The native EditText still draws its Material underline beneath that overlay, so remove it here.
		Microsoft.Maui.Handlers.TimePickerHandler.Mapper.AppendToMapping("RemoveUnderline", (handler, _) =>
		{
			handler.PlatformView.Background = null;
		});
#endif

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
