


namespace Evi
{
    public class Container : Component
    {
        public float Width { get; set; }
        public float Height { get; set; }
        public Color Color { get; set; } = Color.White;
        public float BorderRadius { get; set; } = 0;
        public float Padding { get; set; } = 0;
        public Component? Child { get; set; }
        public Action? OnClick { get; set; }

        public override RenderNode Build()
        {
            ContainerRenderNode node = new()
            {
                Width = Width,
                Height = Height,
                BackgroundColor = Color,
                BorderRadius = BorderRadius,
                Padding = Padding,
                OnPointer = e =>
                {
                    if (e.Type == PointerEventType.Released)
                    {
                        OnClick?.Invoke();
                    }
                }
            };

            if (Child != null)
            {
                node.AddChild(Child.Build());
            }

            return node;
        }
    }

    internal class ContainerRenderNode : RenderNode
    {
        public Color BackgroundColor { get; set; } = Color.Transparent;
        public float BorderRadius { get; set; } = 0;
        public float Padding { get; set; } = 0;
        public Action<PointerEvent>? OnPointer { get; set; }

        public override void Render(IRenderer renderer)
        {
            renderer.Save();
            renderer.Translate(X, Y);

            if (BorderRadius > 0)
            {
                renderer.DrawRoundRect(0, 0, Width, Height, BorderRadius, BackgroundColor);
            }
            else
            {
                renderer.DrawRect(0, 0, Width, Height, BackgroundColor);
            }

            foreach (RenderNode child in Children)
            {
                child.Render(renderer);
            }
            renderer.Restore();
        }

        public override void HandlePointerEvent(PointerEvent e)
        {
            base.HandlePointerEvent(e);
            OnPointer?.Invoke(e);
        }

        public override void Layout(float maxWidth, float maxHeight)
        {
            if (Width == 0) Width = maxWidth;
            if (Height == 0) Height = maxHeight;

            float innerWidth = Width - (Padding * 2);
            float innerHeight = Height - (Padding * 2);

            foreach (RenderNode child in Children)
            {
                child.Layout(innerWidth, innerHeight);
                child.X = Padding;
                child.Y = Padding;
            }
        }
    }
}
