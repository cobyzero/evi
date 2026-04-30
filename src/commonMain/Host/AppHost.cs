

namespace Evi
{
    public abstract class AppHost(Component root)
    {
        protected Component Root { get; set; } = root;

        public abstract void Run();
        public virtual void RequestRedraw() { }

        /// <summary>
        /// Actualiza el componente raíz (Hot Reload) y fuerza un redibujado.
        /// </summary>
        public virtual void HotReload(Component newRoot)
        {
            Root = newRoot;
            RequestRedraw();
        }

        /// <summary>
        /// Renderiza el frame actual usando el árbol de componentes.
        /// </summary>
        internal void RenderFrame(IRenderer renderer, float viewportWidth, float viewportHeight)
        {
            RenderNode renderTree = Root.Build();
            renderTree.Layout(viewportWidth, viewportHeight);
            renderTree.Render(renderer);
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
