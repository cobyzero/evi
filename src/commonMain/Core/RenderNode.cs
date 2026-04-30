


namespace Evi
{
    public abstract class RenderNode
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }
        public int Flex { get; set; } = 0;

        public List<RenderNode> Children { get; } = [];

        public void AddChild(RenderNode child)
        {
            Children.Add(child);
        }

        public abstract void Render(IRenderer renderer);

        public abstract void Layout(float maxWidth, float maxHeight);

        public virtual void HandlePointerEvent(PointerEvent e)
        {
            // Propagar a hijos en orden inverso (del frente hacia atrás)
            for (int i = Children.Count - 1; i >= 0; i--)
            {
                RenderNode child = Children[i];
                if (child.HitTest(e.X - X, e.Y - Y))
                {
                    child.HandlePointerEvent(e with { X = e.X - X, Y = e.Y - Y });
                    return;
                }
            }
        }

        public virtual bool HitTest(float x, float y)
        {
            return x >= 0 && x <= Width && y >= 0 && y <= Height;
        }
    }
}
