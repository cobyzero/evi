#if IOS
using UIKit;
using SkiaSharp;
using SkiaSharp.Views.iOS;
using Evi.Core;
using Evi.Rendering;
using Evi.Host;
using Evi.Components;
using Evi.Core.Events;

namespace Evi.iOS.Host
{
    public class IosHost(Component root) : AppHost(root)
    {



        private EviViewController? _activeController;

        public override void Run()
        {
            // En iOS el host se registra vía UIApplicationDelegate.
        }

        public override void RequestRedraw()
        {
            _activeController?.RequestRedraw();
        }

        public UIViewController CreateViewController()
        {
            _activeController = new EviViewController(this);
            return _activeController;
        }

        private class EviViewController : UIViewController
        {
            private readonly IosHost _host;
            private SKCanvasView? _canvasView;

            public EviViewController(IosHost host)
            {
                _host = host;
            }

            public override void LoadView()
            {
                _canvasView = new SKCanvasView();
                _canvasView.PaintSurface += OnPaintSurface;

                // Habilitar interacción táctil
                _canvasView.UserInteractionEnabled = true;
                _canvasView.AddGestureRecognizer(new UITapGestureRecognizer(OnTap));

                View = _canvasView;
            }

            private void OnPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
            {
                var canvas = e.Surface.Canvas;
                canvas.Clear(SKColors.White);

                var renderer = new SkiaRenderer(canvas);
                _host.RenderFrame(renderer, e.Info.Width, e.Info.Height);
            }

            private void OnTap(UITapGestureRecognizer gesture)
            {
                var point = gesture.LocationInView(_canvasView);

                // Dispatch event to Evi Core
                var renderTree = _host.Root.Build();
                renderTree.Layout((float)_canvasView!.Bounds.Width, (float)_canvasView.Bounds.Height);
                renderTree.HandlePointerEvent(new PointerEvent(
                    (float)point.X,
                    (float)point.Y,
                   PointerEventType.Released));

                _canvasView.SetNeedsDisplay();
            }
            public void RequestRedraw()
            {
                _canvasView?.SetNeedsDisplay();
            }
        }
    }
}

#endif
