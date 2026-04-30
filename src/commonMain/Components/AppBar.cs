namespace Evi
{
    public class AppBar : Component
    {
        public string Title { get; set; } = string.Empty;
        public Color BackgroundColor { get; set; } = new Color(30, 30, 46);
        public Color TextColor { get; set; } = Color.White;
        public float Height { get; set; } = 70;

        public override RenderNode Build()
        {
            return new Container
            {
                Height = Height,
                Color = BackgroundColor,
                Child = new Row
                {
                    CrossAxisAlignment = CrossAxisAlignment.Center,
                    Children =
                    [
                        new Container { Width = 20, Color = Color.Transparent },
                        new Text { Value = Title, Color = TextColor, FontSize = 22 }
                    ]
                }
            }.Build();
        }
    }
}
