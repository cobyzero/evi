


using System.Collections.Generic;

namespace Evi
{
    public abstract class RenderNode
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }
        public int Flex { get; set; } = 0;
        public Component? Creator { get; set; }

        public List<RenderNode> Children { get; } = [];

        public void AddChild(RenderNode child)
        {
            Children.Add(child);
        }

        public void RemoveChild(RenderNode child)
        {
            Children.Remove(child);
        }

        public virtual void CopyPropertiesFrom(RenderNode other)
        {
            // Propiedades base ya se copian en el reconciliador
        }

        public abstract void Render(IRenderer renderer);

        public abstract void Layout(float maxWidth, float maxHeight);

        public virtual void HandleKeyEvent(KeyEvent e)
        {
            foreach (RenderNode child in Children)
            {
                child.HandleKeyEvent(e);
            }
        }

        public virtual void HandlePointerEvent(PointerEvent e)
        {
            // Propagar a hijos en orden inverso (del frente hacia atrás)
            for (int i = Children.Count - 1; i >= 0; i--)
            {
                RenderNode child = Children[i];
                float childLocalX = e.X - child.X;
                float childLocalY = e.Y - child.Y;

                if (child.HitTest(childLocalX, childLocalY))
                {
                    child.HandlePointerEvent(e with { X = childLocalX, Y = childLocalY });
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
