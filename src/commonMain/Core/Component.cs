namespace Evi
{
    public abstract class Component
    {
        internal Element? _element;
        public abstract RenderNode Build();
    }
}
