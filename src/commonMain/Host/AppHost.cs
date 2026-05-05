using System;

namespace Evi
{
    public abstract class AppHost(Component root)
    {
        protected Component Root { get; set; } = root;
        private ComponentElement? _rootElement;

        public abstract void Run();
        public virtual void RequestRedraw() { }

        /// <summary>
        /// Actualiza el componente raíz (Hot Reload) y fuerza un redibujado.
        /// </summary>
        public virtual void HotReload(Component newRoot)
        {
            Root = newRoot;
            _rootElement = null; // Forzar reconstrucción completa
            RequestRedraw();
        }

        /// <summary>
        /// Obtiene o reconstruye el árbol de renderizado actual reconciliado.
        /// </summary>
        public RenderNode GetCurrentRenderTree(float viewportWidth, float viewportHeight)
        {
            if (_rootElement == null)
            {
                _rootElement = new ComponentElement(Root);
                _rootElement.Mount();
            }
            else
            {
                _rootElement.Update(Root);
            }

            RenderNode renderTree = _rootElement.RenderNode!;
            renderTree.Layout(viewportWidth, viewportHeight);
            return renderTree;
        }

        /// <summary>
        /// Renderiza el frame actual usando el árbol de componentes persistente.
        /// </summary>
        public RenderNode RenderFrame(IRenderer renderer, float viewportWidth, float viewportHeight)
        {
            RenderNode renderTree = GetCurrentRenderTree(viewportWidth, viewportHeight);
            renderTree.Render(renderer);
            return renderTree;
        }
    }

    public class ConsoleAppHost(Component root) : AppHost(root)
    {
        public override void Run()
        {
            Console.WriteLine("Iniciando AppHost en modo consola...");
        }
    }
}
