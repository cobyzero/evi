using System;

namespace Evi
{
    public abstract class StatefulComponent : Component
    {
        public abstract State CreateState();

        public override RenderNode Build()
        {
            return new Container().Build(); // Fallback seguro
        }
    }

    public abstract class State
    {
        public StatefulComponent Widget { get; internal set; } = null!;
        internal ComponentElement? _element;

        public virtual void InitState() { }
        public virtual void Dispose() { }

        protected void SetState(Action fn)
        {
            fn();
            _element?.MarkDirty();
        }

        public abstract RenderNode Build();
    }
}
