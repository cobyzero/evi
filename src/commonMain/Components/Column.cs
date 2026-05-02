

namespace Evi
{
    public class Column : Component
    {
        public List<Component> Children { get; set; } = [];
        public MainAxisAlignment MainAxisAlignment { get; set; } = MainAxisAlignment.Start;
        public CrossAxisAlignment CrossAxisAlignment { get; set; } = CrossAxisAlignment.Start;

        public override RenderNode Build()
        {
            ColumnRenderNode node = new()
            {
                MainAxisAlignment = MainAxisAlignment,
                CrossAxisAlignment = CrossAxisAlignment
            };
            foreach (Component child in Children)
            {
                node.AddChild(child.Build());
            }
            return node;
        }
    }

    internal class ColumnRenderNode : RenderNode
    {
        public MainAxisAlignment MainAxisAlignment { get; set; }
        public CrossAxisAlignment CrossAxisAlignment { get; set; }

        public override void CopyPropertiesFrom(RenderNode other)
        {
            if (other is ColumnRenderNode otherColumn)
            {
                MainAxisAlignment = otherColumn.MainAxisAlignment;
                CrossAxisAlignment = otherColumn.CrossAxisAlignment;
            }
        }

        public override void Render(IRenderer renderer)
        {
            renderer.Save();
            renderer.Translate(X, Y);
            foreach (RenderNode child in Children)
            {
                child.Render(renderer);
            }
            renderer.Restore();
        }

        public override void Layout(float maxWidth, float maxHeight)
        {
            float totalFlex = 0;
            float totalFixedHeight = 0;
            float maxChildWidth = 0;

            foreach (RenderNode child in Children)
            {
                if (child.Flex > 0)
                {
                    totalFlex += child.Flex;
                }
                else
                {
                    child.Layout(maxWidth, maxHeight);
                    totalFixedHeight += child.Height;
                    maxChildWidth = Math.Max(maxChildWidth, child.Width);
                }
            }

            float remainingHeight = Math.Max(0, maxHeight - totalFixedHeight);
            float currentY = 0;
            float spacing = 0;

            if (totalFlex == 0)
            {
                switch (MainAxisAlignment)
                {
                    case MainAxisAlignment.Start:
                        currentY = 0;
                        break;
                    case MainAxisAlignment.End:
                        currentY = remainingHeight;
                        break;
                    case MainAxisAlignment.Center:
                        currentY = remainingHeight / 2;
                        break;
                    case MainAxisAlignment.SpaceBetween:
                        if (Children.Count > 1) spacing = remainingHeight / (Children.Count - 1);
                        break;
                    case MainAxisAlignment.SpaceAround:
                        spacing = remainingHeight / Children.Count;
                        currentY = spacing / 2;
                        break;
                    case MainAxisAlignment.SpaceEvenly:
                        spacing = remainingHeight / (Children.Count + 1);
                        currentY = spacing;
                        break;
                }
            }

            foreach (RenderNode child in Children)
            {
                if (child.Flex > 0)
                {
                    float flexHeight = child.Flex / totalFlex * remainingHeight;
                    child.Height = flexHeight;
                    child.Layout(maxWidth, flexHeight);
                }

                child.Y = currentY;
                switch (CrossAxisAlignment)
                {
                    case CrossAxisAlignment.Start:
                        child.X = 0;
                        break;
                    case CrossAxisAlignment.Center:
                        child.X = (maxWidth - child.Width) / 2;
                        break;
                    case CrossAxisAlignment.End:
                        child.X = maxWidth - child.Width;
                        break;
                    case CrossAxisAlignment.Stretch:
                        child.X = 0;
                        child.Width = maxWidth;
                        child.Layout(maxWidth, child.Height);
                        break;
                }

                currentY += child.Height + spacing;
                maxChildWidth = Math.Max(maxChildWidth, child.Width);
            }

            Width = maxWidth;
            Height = maxHeight;
        }
    }
}
