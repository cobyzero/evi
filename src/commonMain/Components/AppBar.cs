namespace Evi
{
    public class AppBar : Component
    {
        public string Title { get; set; } = string.Empty;
        public Component? Leading { get; set; }
        public Color BackgroundColor { get; set; } = new Color(30, 30, 46);
        public Color TextColor { get; set; } = Color.White;
        public float Height { get; set; } = 70;

        public override RenderNode Build()
        {
            var children = new List<Component>();
            children.Add(new Container { Width = 20, Color = Color.Transparent });

            if (Leading != null)
            {
                children.Add(Leading);
                children.Add(new Container { Width = 20, Color = Color.Transparent });
            }

            children.Add(new Text { Value = Title, Color = TextColor, FontSize = 22 });

            return new Container
            {
                Height = Height,
                Color = BackgroundColor,
                Child = new Row
                {
                    CrossAxisAlignment = CrossAxisAlignment.Center,
                    Children = children
                }
            }.Build();
        }
    }
}
