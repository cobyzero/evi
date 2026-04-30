namespace Evi
{
    public class Center : Component
    {
        public Component? Child { get; set; }

        public override RenderNode Build()
        {
            return new Column
            {
                MainAxisAlignment = MainAxisAlignment.Center,
                CrossAxisAlignment = CrossAxisAlignment.Center,
                Children = Child != null ? [Child] : []
            }.Build();
        }
    }
}
