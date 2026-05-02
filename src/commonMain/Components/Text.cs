

namespace Evi
{
    public class Text : Component
    {
        public string Value { get; set; } = string.Empty;
        public float FontSize { get; set; } = 16;
        public Color Color { get; set; } = Color.Black;

        public override RenderNode Build()
        {
            return new TextRenderNode
            {
                Text = Value,
                FontSize = FontSize,
                TextColor = Color
            };
        }
    }

    internal class TextRenderNode : RenderNode
    {
        public string Text { get; set; } = string.Empty;
        public Color TextColor { get; set; } = Color.Black;
        public float FontSize { get; set; } = 16;

        public override void CopyPropertiesFrom(RenderNode other)
        {
            if (other is TextRenderNode otherText)
            {
                Text = otherText.Text;
                TextColor = otherText.TextColor;
                FontSize = otherText.FontSize;
            }
        }

        public override void Render(IRenderer renderer)
        {
            renderer.Save();
            renderer.Translate(X, Y);
            renderer.DrawText(Text, 0, FontSize, FontSize, TextColor);
            renderer.Restore();
        }

        public override void Layout(float maxWidth, float maxHeight)
        {
            Width = maxWidth;
            Height = FontSize;
        }
    }
}
