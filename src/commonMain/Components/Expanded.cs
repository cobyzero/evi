namespace Evi
{
    public class Expanded : Component
    {
        public Component? Child { get; set; }
        public int Flex { get; set; } = 1;

        public override RenderNode Build()
        {
            if (Child == null) throw new InvalidOperationException("Expanded requiere un componente hijo.");
            RenderNode node = Child.Build();
            node.Flex = Flex;
            return node;
        }
    }
}
