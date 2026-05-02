

namespace Evi
{
    public class Row : Component
    {
        public List<Component> Children { get; set; } = [];
        public MainAxisAlignment MainAxisAlignment { get; set; } = MainAxisAlignment.Start;
        public CrossAxisAlignment CrossAxisAlignment { get; set; } = CrossAxisAlignment.Start;

        public override RenderNode Build()
        {
            RowRenderNode node = new()
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

    internal class RowRenderNode : RenderNode
    {
        public MainAxisAlignment MainAxisAlignment { get; set; }
        public CrossAxisAlignment CrossAxisAlignment { get; set; }

        public override void CopyPropertiesFrom(RenderNode other)
        {
            if (other is RowRenderNode otherRow)
            {
                MainAxisAlignment = otherRow.MainAxisAlignment;
                CrossAxisAlignment = otherRow.CrossAxisAlignment;
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
            float totalFixedWidth = 0;
            float maxChildHeight = 0;

            foreach (RenderNode child in Children)
            {
                if (child.Flex > 0)
                {
                    totalFlex += child.Flex;
                }
                else
                {
                    child.Layout(maxWidth, maxHeight);
                    totalFixedWidth += child.Width;
                    maxChildHeight = Math.Max(maxChildHeight, child.Height);
                }
            }

            float remainingWidth = Math.Max(0, maxWidth - totalFixedWidth);
            float currentX = 0;
            float spacing = 0;

            if (totalFlex == 0)
            {
                switch (MainAxisAlignment)
                {
                    case MainAxisAlignment.Start:
                        currentX = 0;
                        break;
                    case MainAxisAlignment.End:
                        currentX = remainingWidth;
                        break;
                    case MainAxisAlignment.Center:
                        currentX = remainingWidth / 2;
                        break;
                    case MainAxisAlignment.SpaceBetween:
                        if (Children.Count > 1) spacing = remainingWidth / (Children.Count - 1);
                        break;
                    case MainAxisAlignment.SpaceAround:
                        spacing = remainingWidth / Children.Count;
                        currentX = spacing / 2;
                        break;
                    case MainAxisAlignment.SpaceEvenly:
                        spacing = remainingWidth / (Children.Count + 1);
                        currentX = spacing;
                        break;
                }
            }

            foreach (RenderNode child in Children)
            {
                if (child.Flex > 0)
                {
                    float flexWidth = child.Flex / totalFlex * remainingWidth;
                    child.Width = flexWidth;
                    child.Layout(flexWidth, maxHeight);
                }

                child.X = currentX;
                switch (CrossAxisAlignment)
                {
                    case CrossAxisAlignment.Start:
                        child.Y = 0;
                        break;
                    case CrossAxisAlignment.Center:
                        child.Y = (maxHeight - child.Height) / 2;
                        break;
                    case CrossAxisAlignment.End:
                        child.Y = maxHeight - child.Height;
                        break;
                    case CrossAxisAlignment.Stretch:
                        child.Y = 0;
                        child.Height = maxHeight;
                        child.Layout(child.Width, maxHeight);
                        break;
                }

                currentX += child.Width + spacing;
                maxChildHeight = Math.Max(maxChildHeight, child.Height);
            }

            Width = maxWidth;
            Height = maxHeight;
        }
    }
}
