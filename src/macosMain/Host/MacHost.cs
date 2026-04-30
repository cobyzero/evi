#if !IOS
using Silk.NET.Windowing;
using Silk.NET.Input;
using Silk.NET.OpenGL;
using SkiaSharp;

namespace Evi
{
    public class MacHost(Component root) : AppHost(root), IDisposable
    {
        private IWindow? _window;
        private GL? _gl;
        private GRContext? _grContext;
        private SKSurface? _surface;
        private GRBackendRenderTarget? _renderTarget;

        // Hot Reload: flag para reconstruir la UI en el próximo frame del loop de Silk.NET
        private volatile bool _pendingHotReload = false;
        private Component? _pendingRoot = null;
        private readonly object _hotReloadLock = new();

        public override void Run()
        {
            WindowOptions options = WindowOptions.Default;
            options.Size = new Silk.NET.Maths.Vector2D<int>(800, 600);
            options.Title = "Evi UI Framework - macOS";
            options.PreferredBitDepth = new Silk.NET.Maths.Vector4D<int>(8, 8, 8, 8);
            options.Samples = 4;

            _window = Window.Create(options);

            _window.Load += OnLoad;
            _window.Render += OnRender;
            _window.Resize += OnResize;
            _window.Closing += OnClosing;

            _window.Run();
        }

        private void OnLoad()
        {
            _gl = _window!.CreateOpenGL();

            GRGlInterface interface_ = GRGlInterface.Create();
            _grContext = GRContext.CreateGl(interface_);

            IInputContext input = _window!.CreateInput();
            foreach (IMouse mouse in input.Mice)
            {
                mouse.MouseDown += (m, b) => DispatchPointerEvent(m.Position.X, m.Position.Y, PointerEventType.Pressed);
                mouse.MouseUp += (m, b) => DispatchPointerEvent(m.Position.X, m.Position.Y, PointerEventType.Released);
                mouse.MouseMove += (m, pos) => DispatchPointerEvent(pos.X, pos.Y, PointerEventType.Moved);
            }

            CreateSurface();
        }

        private void DispatchPointerEvent(float x, float y, PointerEventType type)
        {
            RenderNode renderTree = Root.Build();
            renderTree.Layout(_window!.Size.X, _window.Size.Y);
            renderTree.HandlePointerEvent(new PointerEvent(x, y, type));
        }

        private void OnResize(Silk.NET.Maths.Vector2D<int> size)
        {
            CreateSurface();
        }

        private void CreateSurface()
        {
            _surface?.Dispose();
            _renderTarget?.Dispose();

            Silk.NET.Maths.Vector2D<int> size = _window!.Size;

            _renderTarget = new GRBackendRenderTarget(size.X, size.Y, 0, 8, new GRGlFramebufferInfo(0, 0x8058));
            _surface = SKSurface.Create(_grContext, _renderTarget, GRSurfaceOrigin.BottomLeft, SKColorType.Rgba8888);
        }

        private void OnRender(double delta)
        {
            if (_surface == null || _window == null)
                return;

            // Aplicar Hot Reload si hay un root pendiente (thread-safe)
            if (_pendingHotReload)
            {
                lock (_hotReloadLock)
                {
                    if (_pendingHotReload && _pendingRoot != null)
                    {
                        Root = _pendingRoot;
                        _pendingRoot = null;
                        _pendingHotReload = false;
                        Console.WriteLine("[Evi] 🔥 Hot Reload aplicado.");
                    }
                }
            }

            _surface.Canvas.Clear(SKColors.White);

            SkiaRenderer skiaRenderer = new(_surface.Canvas);
            RenderFrame(skiaRenderer, _window.Size.X, _window.Size.Y);

            _grContext!.Flush();
        }

        private void OnClosing()
        {
            Dispose();
        }

        /// <summary>
        /// Hot Reload: encola el nuevo root para ser aplicado de forma segura
        /// en el hilo del render loop de Silk.NET en el próximo frame.
        /// </summary>
        public override void HotReload(Component newRoot)
        {
            lock (_hotReloadLock)
            {
                _pendingRoot = newRoot;
                _pendingHotReload = true;
            }
        }

        public override void RequestRedraw()
        {
            // El render loop de Silk.NET ya redibuja continuamente; no hace falta nada extra.
        }

        public void Dispose()
        {
            _surface?.Dispose();
            _renderTarget?.Dispose();
            _grContext?.Dispose();
            _gl?.Dispose();
        }
    }
}

#endif