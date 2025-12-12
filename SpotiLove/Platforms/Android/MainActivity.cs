using Android.App;
using Android.Content.PM;
using Android.OS;

namespace SpotiLove;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnNewIntent(Android.Content.Intent? intent)
    {
        base.OnNewIntent(intent);

        System.Diagnostics.Debug.WriteLine($"🔗 OnNewIntent called!");
        System.Diagnostics.Debug.WriteLine($"   Action: {intent?.Action}");
        System.Diagnostics.Debug.WriteLine($"   Data: {intent?.Data}");
        System.Diagnostics.Debug.WriteLine($"   DataString: {intent?.DataString}");
    }
}
