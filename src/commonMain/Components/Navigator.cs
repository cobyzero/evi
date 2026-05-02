using System;
using System.Collections.Generic;

namespace Evi
{
    public class Navigator : StatefulComponent
    {
        private readonly Stack<Component> _history = new();
        private static Navigator? _instance;

        public Navigator(Component initialPage)
        {
            _history.Push(initialPage);
            _instance = this;
        }

        public static void Push(Component page)
        {
            _instance?._navigatorState?.Push(page);
        }

        public static void Pop()
        {
            _instance?._navigatorState?.Pop();
        }

        private NavigatorState? _navigatorState => (NavigatorState?)((ComponentElement?)_instance?._element)?._state;

        public override State CreateState() => new NavigatorState();

        private class NavigatorState : State
        {
            public void Push(Component page)
            {
                SetState(() => {
                    ((Navigator)Widget)._history.Push(page);
                });
            }

            public void Pop()
            {
                SetState(() => {
                    var history = ((Navigator)Widget)._history;
                    if (history.Count > 1) history.Pop();
                });
            }

            public override RenderNode Build()
            {
                var history = ((Navigator)Widget)._history;
                if (history.Count == 0) return new Container().Build();
                
                Component page = history.Peek();
                if (page is StatefulComponent stateful)
                {
                    return BypassBuild(stateful);
                }
                
                return page.Build();
            }

            private RenderNode BypassBuild(StatefulComponent stateful)
            {
                var s = stateful.CreateState();
                s.Widget = stateful;
                s._element = _element; 
                return s.Build();
            }
        }
    }
}
