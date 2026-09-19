using AndroidX.Biometric;
using AndroidX.Core.Content;
using AndroidX.Fragment.App;
using Microsoft.Maui.ApplicationModel;

namespace WeighIn.Platforms.Android;

public static class BiometricAuthenticator
{
    public static bool IsAvailable()
    {
        var context = global::Android.App.Application.Context;
        var manager = BiometricManager.From(context);
        var result = manager.CanAuthenticate(BiometricManager.Authenticators.BiometricWeak);
        return result == BiometricManager.BiometricSuccess;
    }

    public static Task<bool> AuthenticateAsync(string title, string subtitle)
    {
        var tcs = new TaskCompletionSource<bool>();

        if (Platform.CurrentActivity is not FragmentActivity activity)
        {
            tcs.SetResult(false);
            return tcs.Task;
        }

        if (ContextCompat.GetMainExecutor(activity) is not { } executor)
        {
            tcs.SetResult(false);
            return tcs.Task;
        }

        var callback = new AuthCallback(tcs);
        var prompt = new BiometricPrompt(activity, executor, callback);

        var promptInfo = new BiometricPrompt.PromptInfo.Builder()
            .SetTitle(title)
            .SetSubtitle(subtitle)
            .SetNegativeButtonText("Use PIN instead")
            .SetAllowedAuthenticators(BiometricManager.Authenticators.BiometricWeak)
            .Build();

        prompt.Authenticate(promptInfo);
        return tcs.Task;
    }

    private sealed class AuthCallback(TaskCompletionSource<bool> tcs) : BiometricPrompt.AuthenticationCallback
    {
        public override void OnAuthenticationSucceeded(BiometricPrompt.AuthenticationResult result)
        {
            base.OnAuthenticationSucceeded(result);
            tcs.TrySetResult(true);
        }

        public override void OnAuthenticationError(int errorCode, Java.Lang.ICharSequence errString)
        {
            base.OnAuthenticationError(errorCode, errString);
            tcs.TrySetResult(false);
        }
    }
}
