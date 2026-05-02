namespace Evi
{
    public record BoxShadow
    {
        public float OffsetX { get; init; } = 0;
        public float OffsetY { get; init; } = 4;
        public float Blur { get; init; } = 10;
        public Color Color { get; init; } = new Color(0, 0, 0, 50);

        public BoxShadow(float offsetX = 0, float offsetY = 4, float blur = 10, Color? color = null)
        {
            OffsetX = offsetX;
            OffsetY = offsetY;
            Blur = blur;
            Color = color ?? new Color(0, 0, 0, 50);
        }
    }
}
