namespace Evi
{
    public class FloatingActionButton : Component
    {
        public Component? Child { get; set; }
        public Color BackgroundColor { get; set; } = new Color(137, 80, 255);
        public Action? OnPressed { get; set; }

        public override RenderNode Build()
        {
            return new Container
            {
                Width = 60,
                Height = 60,
                Color = BackgroundColor,
                BorderRadius = 30,
                OnClick = OnPressed,
                Child = Child
            }.Build();
        }
    }
}
