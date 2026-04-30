namespace Evi
{
    public class Scaffold : Component
    {
        public Component? AppBar { get; set; }
        public Component? Body { get; set; }
        public Component? FloatingActionButton { get; set; }
        public Color BackgroundColor { get; set; } = Color.White;

        public override RenderNode Build()
        {
            ScaffoldRenderNode node = new ScaffoldRenderNode
            {
                BackgroundColor = BackgroundColor
            };

            if (AppBar != null)
            {
                RenderNode appBarNode = AppBar.Build();
                node.AppBar = appBarNode;
                node.AddChild(appBarNode);
            }

            if (Body != null)
            {
                RenderNode bodyNode = Body.Build();
                node.Body = bodyNode;
                node.AddChild(bodyNode);
            }

            if (FloatingActionButton != null)
            {
                RenderNode fabNode = FloatingActionButton.Build();
                node.FloatingActionButton = fabNode;
                node.AddChild(fabNode);
            }

            return node;
        }
    }

    internal class ScaffoldRenderNode : RenderNode
    {
        public Color BackgroundColor { get; set; }
        public RenderNode? AppBar { get; set; }
        public RenderNode? Body { get; set; }
        public RenderNode? FloatingActionButton { get; set; }

        public override void Render(IRenderer renderer)
        {
            renderer.Save();
            renderer.Translate(X, Y);

            renderer.DrawRect(0, 0, Width, Height, BackgroundColor);

            AppBar?.Render(renderer);
            Body?.Render(renderer);
            FloatingActionButton?.Render(renderer);

            renderer.Restore();
        }

        public override void Layout(float maxWidth, float maxHeight)
        {
            Width = maxWidth;
            Height = maxHeight;

            float currentY = 0;

            if (AppBar != null)
            {
                AppBar.Layout(maxWidth, maxHeight);
                AppBar.X = 0;
                AppBar.Y = 0;
                currentY = AppBar.Height;
            }

            if (Body != null)
            {
                float bodyHeight = maxHeight - currentY;
                Body.Layout(maxWidth, bodyHeight);
                Body.X = 0;
                Body.Y = currentY;
                Body.Width = maxWidth;
                Body.Height = bodyHeight;
            }

            if (FloatingActionButton != null)
            {
                FloatingActionButton.Layout(maxWidth, maxHeight);
                FloatingActionButton.X = maxWidth - FloatingActionButton.Width - 20;
                FloatingActionButton.Y = maxHeight - FloatingActionButton.Height - 20;
            }
        }

        public override void HandlePointerEvent(PointerEvent e)
        {
            PointerEvent localEvent = e with { X = e.X - X, Y = e.Y - Y };

            if (FloatingActionButton != null && FloatingActionButton.HitTest(localEvent.X, localEvent.Y))
            {
                FloatingActionButton.HandlePointerEvent(localEvent);
                return;
            }

            if (AppBar != null && AppBar.HitTest(localEvent.X, localEvent.Y))
            {
                AppBar.HandlePointerEvent(localEvent);
                return;
            }

            if (Body != null && Body.HitTest(localEvent.X, localEvent.Y))
            {
                Body.HandlePointerEvent(localEvent);
                return;
            }
        }
    }
}
