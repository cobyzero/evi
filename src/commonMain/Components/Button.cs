using System;

namespace Evi
{
    public class Button : Component
    {
        public string Text { get; set; } = string.Empty;
        public Action? OnPressed { get; set; }
        public Color BackgroundColor { get; set; } = new Color(137, 80, 255);
        public Color HoverColor { get; set; } = new Color(157, 110, 255);
        public Color PressedColor { get; set; } = new Color(107, 50, 215);
        public Color TextColor { get; set; } = Color.White;
        public float Width { get; set; } = 250;
        public float Height { get; set; } = 55;
        public float BorderRadius { get; set; } = 16;
        public float BorderWidth { get; set; } = 0;
        public Color BorderColor { get; set; } = Color.Transparent;
        public float FontSize { get; set; } = 18;
        public BoxShadow? Shadow { get; set; } = new BoxShadow(0, 4, 12, new Color(0, 0, 0, 40));
        public object? Identity { get; set; }

        public override RenderNode Build()
        {
            object identity = Identity ?? Text;

            bool isHovered = HoverManager.IsHovered(identity);
            bool isPressed = PointerManager.IsPressed(identity);
            
            Color currentColor = isPressed ? PressedColor : (isHovered ? HoverColor : BackgroundColor);
            
            // Efecto de escala o sombra más pequeña al presionar
            BoxShadow? currentShadow = isPressed 
                ? new BoxShadow(0, 2, 8, new Color(0, 0, 0, 60))
                : (isHovered ? new BoxShadow(0, 8, 24, new Color(currentColor.R, currentColor.G, currentColor.B, 80)) : Shadow);

            return new Container
            {
                Identity = identity,
                Width = Width,
                Height = Height,
                Color = currentColor,
                BorderRadius = BorderRadius,
                BorderWidth = BorderWidth,
                BorderColor = BorderColor,
                Shadow = currentShadow,
                OnClick = OnPressed,
                Child = new Center
                {
                    Child = new Text
                    {
                        Value = Text,
                        Color = TextColor,
                        FontSize = FontSize
                    }
                }
            }.Build();
        }
    }
}
