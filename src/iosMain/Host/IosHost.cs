#if IOS
using UIKit;
using Foundation;
using CoreAnimation;
using SkiaSharp;
using SkiaSharp.Views.iOS;
using Evi;

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
#if DEBUG
            private CADisplayLink? _displayLink;
#endif

            public EviViewController(IosHost host)
            {
                _host = host;
            }

            public override void LoadView()
            {
                _canvasView = new EviCanvasView(_host);
                _canvasView.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;
                View = _canvasView;
            }

            public override void ViewDidLayoutSubviews()
            {
                base.ViewDidLayoutSubviews();
                _canvasView?.SetNeedsDisplay();
            }

            public void RequestRedraw()
            {
                _canvasView?.SetNeedsDisplay();
            }

#if DEBUG
            public override void ViewDidAppear(bool animated)
            {
                base.ViewDidAppear(animated);

                if (_displayLink == null)
                {
                    _displayLink = CADisplayLink.Create(() =>
                    {
                        _canvasView?.SetNeedsDisplay();
                    });
                    _displayLink.PreferredFramesPerSecond = 4;
                    _displayLink.AddToRunLoop(NSRunLoop.Main, NSRunLoopMode.Common);
                }
            }

            public override void ViewWillDisappear(bool animated)
            {
                base.ViewWillDisappear(animated);
                _displayLink?.Invalidate();
                _displayLink = null;
            }
#endif
        }

        private class EviCanvasView : SKCanvasView
        {
            private readonly IosHost _host;

            public EviCanvasView(IosHost host)
            {
                _host = host;
                UserInteractionEnabled = true;
                IgnorePixelScaling = true;
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

                // Con IgnorePixelScaling=true trabajamos en coordenadas lógicas (puntos UIKit).
                var viewportWidth = (float)Bounds.Width;
                var viewportHeight = (float)Bounds.Height;

                var renderer = new SkiaRenderer(canvas);
                _host.RenderFrame(renderer, viewportWidth, viewportHeight);
            }
        }
    }
}

#endif
