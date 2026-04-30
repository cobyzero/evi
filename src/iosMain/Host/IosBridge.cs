#if IOS
using UIKit;
using Evi.Core;
using Evi.Host;
using Evi.Components;

namespace Evi.iOS.Host
{
    /// <summary>
    /// Puente interno entre App.Run() y el sistema iOS.
    /// El usuario nunca interactúa con esta clase directamente.
    /// </summary>
    internal static class IosBridge
    {
        internal static Component? Root { get; private set; }

        internal static void SetRoot(Component root)
        {
            Root = root;
        }
    }

    [Register("EviAppDelegate")]
    internal class EviAppDelegate : UIApplicationDelegate
    {
        public override UIWindow? Window { get; set; }

        public override bool FinishedLaunching(UIApplication application, NSDictionary? launchOptions)
        {
            Window = new UIWindow(UIScreen.MainScreen.Bounds);

            if (IosBridge.Root != null)
            {
                var host = new IosHost(IosBridge.Root);
                Window.RootViewController = host.CreateViewController();
            }

            Window.MakeKeyAndVisible();
            return true;
        }
    }
}
#endif
