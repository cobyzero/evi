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

        public override void HotReload(Component newRoot)
        {
            Root = newRoot;
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
            private EviCanvasView? _canvasView;

            public EviViewController(IosHost host)
            {
                _host = host;
            }

            public override void LoadView()
            {
                _canvasView = new EviCanvasView(_host);
                View = _canvasView;
            }

            public void RequestRedraw()
            {
                _canvasView?.SetNeedsDisplay();
            }
        }

        private class EviCanvasView : SKCanvasView
        {
            private readonly IosHost _host;

            public EviCanvasView(IosHost host)
            {
                _host = host;
                UserInteractionEnabled = true;
            }

            public override void TouchesBegan(Foundation.NSSet touches, UIEvent? evt)
            {
                DispatchTouch(touches, PointerEventType.Pressed);
            }

            public override void TouchesMoved(Foundation.NSSet touches, UIEvent? evt)
            {
                DispatchTouch(touches, PointerEventType.Moved);
            }

            public override void TouchesEnded(Foundation.NSSet touches, UIEvent? evt)
            {
                DispatchTouch(touches, PointerEventType.Released);
            }

            private void DispatchTouch(Foundation.NSSet touches, PointerEventType type)
            {
                if (touches.AnyObject is UITouch touch)
                {
                    var point = touch.LocationInView(this);
                    
                    // Dispatch to Evi Core
                    var renderTree = _host.GetCurrentRenderTree((float)Bounds.Width, (float)Bounds.Height);
                    renderTree.HandlePointerEvent(new PointerEvent(
                        (float)point.X,
                        (float)point.Y,
                        type));

                    SetNeedsDisplay();
                }
            }

            protected override void OnPaintSurface(SKPaintSurfaceEventArgs e)
            {
                base.OnPaintSurface(e);
                var canvas = e.Surface.Canvas;
                canvas.Clear(SKColors.White);

                var renderer = new SkiaRenderer(canvas);
                _host.RenderFrame(renderer, (float)Bounds.Width, (float)Bounds.Height);
            }
        }
    }
}

#endif
