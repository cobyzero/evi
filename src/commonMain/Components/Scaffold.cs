namespace Evi
{
    public class Scaffold : Component
    {
        public Component? AppBar { get; set; }
        public Component? Body { get; set; }
        public Component? Drawer { get; set; }
        public bool IsDrawerOpen { get; set; } = false;
        public Action? OnCloseDrawer { get; set; }
        public Component? FloatingActionButton { get; set; }
        public Color BackgroundColor { get; set; } = Color.White;

        public override RenderNode Build()
        {
            ScaffoldRenderNode node = new ScaffoldRenderNode
            {
                BackgroundColor = BackgroundColor,
                IsDrawerOpen = IsDrawerOpen,
                OnCloseDrawer = OnCloseDrawer
            };

            if (AppBar != null)
            {
                RenderNode appBarNode = AppBar.Build();
                node.AppBar = appBarNode;
                node.AddChild(appBarNode);
            }

            if (Body != null)
            {
                RenderNode bodyNode = Body.Build();
                node.Body = bodyNode;
                node.AddChild(bodyNode);
            }

            if (FloatingActionButton != null)
            {
                RenderNode fabNode = FloatingActionButton.Build();
                node.FloatingActionButton = fabNode;
                node.AddChild(fabNode);
            }

            if (Drawer != null)
            {
                RenderNode drawerNode = Drawer.Build();
                node.Drawer = drawerNode;
                node.AddChild(drawerNode);
            }

            return node;
        }
    }

    internal class ScaffoldRenderNode : RenderNode
    {
        public Color BackgroundColor { get; set; }
        public RenderNode? AppBar { get; set; }
        public RenderNode? Body { get; set; }
        public RenderNode? FloatingActionButton { get; set; }
        public RenderNode? Drawer { get; set; }
        public bool IsDrawerOpen { get; set; }
        public Action? OnCloseDrawer { get; set; }

        public override void CopyPropertiesFrom(RenderNode other)
        {
            if (other is ScaffoldRenderNode otherScaffold)
            {
                BackgroundColor = otherScaffold.BackgroundColor;
                IsDrawerOpen = otherScaffold.IsDrawerOpen;
                OnCloseDrawer = otherScaffold.OnCloseDrawer;
                
                // Re-mapeamos los campos a los hijos reconciliados
                // Orden: AppBar, Body, FAB, Drawer
                AppBar = Children.Count > 0 ? Children[0] : null;
                Body = Children.Count > 1 ? Children[1] : null;
                FloatingActionButton = Children.Count > 2 ? Children[2] : null;
                Drawer = Children.Count > 3 ? Children[3] : null;
            }
        }

        public override void Render(IRenderer renderer)
        {
            renderer.Save();
            renderer.Translate(X, Y);

            renderer.Clear(BackgroundColor);

            AppBar?.Render(renderer);
            Body?.Render(renderer);
            FloatingActionButton?.Render(renderer);

            if (IsDrawerOpen)
            {
                // Scrim (fondo semi-transparente)
                renderer.DrawRect(0, 0, Width, Height, new Color(0, 0, 0, 100));
                
                // Drawer
                if (Drawer != null)
                {
                    // Agregamos una sombra artificial detrás del drawer para que se vea por encima
                    renderer.DrawRect(Drawer.X, Drawer.Y, Drawer.Width, Drawer.Height, Color.Transparent, 
                                      new BoxShadow(10, 0, 30, new Color(0, 0, 0, 80)));
                    Drawer.Render(renderer);
                }
            }

            renderer.Restore();
        }

        public override void Layout(float maxWidth, float maxHeight)
        {
            Width = maxWidth;
            Height = maxHeight;

            float currentY = 0;

            if (AppBar != null)
            {
                AppBar.Layout(maxWidth, maxHeight);
                AppBar.X = 0;
                AppBar.Y = 0;
                currentY = AppBar.Height;
            }

            if (Body != null)
            {
                float bodyHeight = maxHeight - currentY;
                Body.Layout(maxWidth, bodyHeight);
                Body.X = 0;
                Body.Y = currentY;
                Body.Width = maxWidth;
                Body.Height = bodyHeight;
            }

            if (FloatingActionButton != null)
            {
                FloatingActionButton.Layout(maxWidth, maxHeight);
                FloatingActionButton.X = maxWidth - FloatingActionButton.Width - 20;
                FloatingActionButton.Y = maxHeight - FloatingActionButton.Height - 20;
            }

            if (Drawer != null)
            {
                float drawerWidth = 320; // Ancho estándar del drawer
                Drawer.Layout(drawerWidth, maxHeight);
                Drawer.X = IsDrawerOpen ? 0 : -drawerWidth;
                Drawer.Y = 0;
                Drawer.Height = maxHeight;
                Drawer.Width = drawerWidth;
            }
        }

        public override void HandlePointerEvent(PointerEvent e)
        {
            if (IsDrawerOpen)
            {
                if (Drawer != null)
                {
                    float drawerLocalX = e.X - Drawer.X;
                    float drawerLocalY = e.Y - Drawer.Y;
                    if (Drawer.HitTest(drawerLocalX, drawerLocalY))
                    {
                        Drawer.HandlePointerEvent(e with { X = drawerLocalX, Y = drawerLocalY });
                        return;
                    }
                }

                // Si se hace clic fuera del drawer, cerrarlo
                if (e.Type == PointerEventType.Pressed)
                {
                    OnCloseDrawer?.Invoke();
                    return;
                }
                
                // Bloquear eventos hacia el fondo mientras el drawer está abierto
                return;
            }

            if (FloatingActionButton != null)
            {
                float fabLocalX = e.X - FloatingActionButton.X;
                float fabLocalY = e.Y - FloatingActionButton.Y;
                if (FloatingActionButton.HitTest(fabLocalX, fabLocalY))
                {
                    FloatingActionButton.HandlePointerEvent(e with { X = fabLocalX, Y = fabLocalY });
                    return;
                }
            }

            if (AppBar != null)
            {
                float appLocalX = e.X - AppBar.X;
                float appLocalY = e.Y - AppBar.Y;
                if (AppBar.HitTest(appLocalX, appLocalY))
                {
                    AppBar.HandlePointerEvent(e with { X = appLocalX, Y = appLocalY });
                    return;
                }
            }

            if (Body != null)
            {
                float bodyLocalX = e.X - Body.X;
                float bodyLocalY = e.Y - Body.Y;
                if (Body.HitTest(bodyLocalX, bodyLocalY))
                {
                    Body.HandlePointerEvent(e with { X = bodyLocalX, Y = bodyLocalY });
                    return;
                }
            }
        }
    }
}
