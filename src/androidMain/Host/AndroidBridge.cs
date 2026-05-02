#if ANDROID
using Android.App;
using Android.OS;
using Android.Content.PM;
using Evi.Core;

namespace Evi.Android.Host;

/// <summary>
/// Puente interno entre App.Run() y el sistema Android.
/// </summary>
internal static class AndroidBridge
{
    internal static Component? Root { get; private set; }

    internal static void SetRoot(Component root)
    {
        Root = root;
    }
}

[Activity(Label = "Evi App", 
          MainLauncher = true, 
          ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize)]
public class EviActivity : Activity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        if (AndroidBridge.Root != null)
        {
            var host = new AndroidHost(this, AndroidBridge.Root);
            App.CurrentHost = host;
            SetContentView(host.View);
        }
    }
}
#endif
