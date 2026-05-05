#if ANDROID
using Android.Content;
using Android.Views;
using SkiaSharp.Views.Android;
using Evi;

namespace Evi.Android.Host;

public class AndroidHost : AppHost
{
    private readonly EviCanvasView _canvasView;
    private int _width;
    private int _height;

    public AndroidHost(Context context, Component root) : base(root)
    {
        _canvasView = new EviCanvasView(context, this);
    }

    public View View => _canvasView;

    public override void Run()
    {
        // En Android, el Activity se encarga de mostrar la vista.
    }

    public override void RequestRedraw()
    {
        _canvasView.Invalidate();
    }

    public void OnRender(SKCanvasView canvasView, SKPaintSurfaceEventArgs e)
    {
        _width = e.Info.Width;
        _height = e.Info.Height;
        
        var renderer = new SkiaRenderer(e.Surface.Canvas);
        RenderFrame(renderer, _width, _height);
    }

    public override void HotReload(Component newRoot)
    {
        Root = newRoot;
        RequestRedraw();
    }

    private class EviCanvasView : SKCanvasView
    {
        private readonly AndroidHost _host;

        public EviCanvasView(Context context, AndroidHost host) : base(context)
        {
            _host = host;
            PaintSurface += OnPaintSurface;
        }

        private void OnPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
        {
            _host.OnRender(this, e);
        }

        public override bool OnTouchEvent(MotionEvent? e)
        {
            if (e == null) return false;

            var type = e.Action switch
            {
                MotionEventActions.Down => PointerEventType.Pressed,
                MotionEventActions.Move => PointerEventType.Moved,
                MotionEventActions.Up => PointerEventType.Released,
                _ => (PointerEventType?)null
            };

            if (type.HasValue)
            {
                if (type == PointerEventType.Pressed)
                    TextFieldRenderNode.ClearFocus();

                var renderTree = _host.GetCurrentRenderTree(_host._width, _host._height);
                renderTree.HandlePointerEvent(new PointerEvent(e.GetX(), e.GetY(), type.Value));
                _host.RequestRedraw();
                return true;
            }

            return base.OnTouchEvent(e);
        }
    }
}
#endif
