using System.Reflection.Metadata;
#if IOS
using Evi.iOS.Host;
#endif

[assembly: MetadataUpdateHandler(typeof(Evi.HotReloadManager))]

namespace Evi
{
    /// <summary>
    /// Punto de entrada del framework Evi.
    /// Los usuarios llaman a App.Run(() => widget) para iniciar su aplicación.
    /// </summary>
    public static class App
    {
        public static AppHost? CurrentHost { get; set; }
        internal static Func<Component>? RootFactory { get; private set; }

        /// <summary>
        /// Inicia la aplicación Evi con una factory del componente raíz.
        /// La factory se invoca de nuevo en cada Hot Reload para reconstruir la UI.
        /// </summary>
        /// <param name="rootFactory">Factory que devuelve el componente raíz.</param>
        public static void Run(Func<Component> rootFactory)
        {
            RootFactory = rootFactory;
            Component root = rootFactory();

#if IOS
            IosBridge.SetRoot(root);
            UIKit.UIApplication.Main([], null, typeof(EviAppDelegate));
#elif ANDROID
            Evi.Android.Host.AndroidBridge.SetRoot(root);
#elif WEB
            using WebHost host = new(root);
            CurrentHost = host;
            host.Run();
            CurrentHost = null;
#else
            using MacHost host = new(root);
            CurrentHost = host;
            host.Run();
            CurrentHost = null;
#endif
        }

        /// <summary>
        /// Sobrecarga de conveniencia: acepta un Component directamente.
        /// Nota: Hot Reload solo actualiza métodos, no instancias. Para aprovechar
        /// el Hot Reload de cambios visuales, usa la sobrecarga con Func&lt;Component&gt;.
        /// </summary>
        public static void Run(Component root) => Run(() => root);
    }

    public static class HotReloadManager
    {
        /// <summary>
        /// Llamado automáticamente por el runtime de .NET cuando detecta
        /// cambios de metadatos (Hot Reload). Reconstruye el árbol de UI
        /// invocando de nuevo la RootFactory y fuerza un redibujado.
        /// </summary>
        public static void UpdateApplication(Type[]? types)
        {
            if (App.CurrentHost == null || App.RootFactory == null)
            {
                Console.WriteLine($"[Evi Hot Reload] Skipped. Host null: {App.CurrentHost == null}, RootFactory null: {App.RootFactory == null}");
                return;
            }

            try
            {
                Console.WriteLine($"[Evi Hot Reload] Applying update. Types: {types?.Length ?? 0}");
                Component newRoot = App.RootFactory();
                App.CurrentHost.HotReload(newRoot);
                Console.WriteLine("[Evi Hot Reload] UI redraw requested.");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[Evi Hot Reload] Error al reconstruir la UI: {ex.Message}");
            }
        }
    }
}
