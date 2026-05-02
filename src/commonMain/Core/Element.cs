using System;
using System.Collections.Generic;

namespace Evi
{
    public abstract class Element
    {
        public Component Component { get; protected set; }
        public RenderNode? RenderNode { get; protected set; }

        protected Element(Component component)
        {
            Component = component;
            Component._element = this;
        }

        public abstract void Update(Component newComponent);
        public abstract void Mount();
        public abstract void Rebuild();
    }

    public class ComponentElement : Element
    {
        internal State? _state;

        public ComponentElement(Component component) : base(component) { }

        public override void Mount()
        {
            if (Component is StatefulComponent stateful)
            {
                _state = stateful.CreateState();
                _state.Widget = stateful;
                _state._element = this;
                _state.InitState();
                RenderNode = _state.Build();
            }
            else
            {
                RenderNode = Component.Build();
            }
        }

        public void MarkDirty()
        {
            Rebuild();
        }

        public override void Rebuild()
        {
            RenderNode newNode = (_state != null) ? _state.Build() : Component.Build();
            
            if (RenderNode == null || RenderNode.GetType() != newNode.GetType())
            {
                RenderNode = newNode;
            }
            else
            {
                ReconcileRenderNodes(RenderNode, newNode);
            }
        }

        public override void Update(Component newComponent)
        {
            if (Component.GetType() != newComponent.GetType())
            {
                // Si el tipo de componente cambia, necesitamos remontar
                Component = newComponent;
                Component._element = this;
                Mount();
                return;
            }

            Component = newComponent;
            Component._element = this;
            if (_state != null)
            {
                _state.Widget = (StatefulComponent)Component;
            }
            
            Rebuild();
        }

        private void ReconcileRenderNodes(RenderNode oldNode, RenderNode newNode)
        {
            if (oldNode.GetType() != newNode.GetType()) return;

            oldNode.X = newNode.X;
            oldNode.Y = newNode.Y;
            oldNode.Width = newNode.Width;
            oldNode.Height = newNode.Height;
            oldNode.Flex = newNode.Flex;

            oldNode.CopyPropertiesFrom(newNode);

            int oldSize = oldNode.Children.Count;
            int newSize = newNode.Children.Count;

            for (int i = 0; i < newSize; i++)
            {
                if (i < oldSize)
                {
                    RenderNode oldChild = oldNode.Children[i];
                    RenderNode newChild = newNode.Children[i];

                    if (oldChild.GetType() == newChild.GetType())
                    {
                        ReconcileRenderNodes(oldChild, newChild);
                    }
                    else
                    {
                        oldNode.Children[i] = newChild;
                    }
                }
                else
                {
                    oldNode.AddChild(newNode.Children[i]);
                }
            }

            if (oldSize > newSize)
            {
                int countToRemove = oldSize - newSize;
                for (int i = 0; i < countToRemove; i++)
                {
                    oldNode.RemoveChild(oldNode.Children[oldNode.Children.Count - 1]);
                }
            }
        }
    }
}
