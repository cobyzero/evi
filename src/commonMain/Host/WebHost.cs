#if WEB
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using SkiaSharp;

namespace Evi;

/// <summary>
/// Host web que renderiza la UI de Evi en el navegador usando un canvas HTML5.
/// Cada frame se renderiza con Skia a un PNG y se envía por WebSocket.
/// Los eventos de mouse del navegador se reenvían al Core de Evi.
/// </summary>
public class WebHost(Component root) : AppHost(root), IDisposable
{
    private readonly HttpListener _listener = new();
    private readonly List<WebSocket> _clients = [];
    private readonly object _clientsLock = new();
    private int _width = 1280;
    private int _height = 720;
    private volatile bool _running;
    private volatile bool _dirty = true;

    public int Port { get; init; } = 5050;

    public override void Run()
    {
        _listener.Prefixes.Add($"http://localhost:{Port}/");
        _listener.Start();
        _running = true;

        Console.WriteLine($"[Evi Web] 🌐 Servidor en http://localhost:{Port}");

        // Abrimos el navegador automáticamente
        OpenBrowser($"http://localhost:{Port}");

        // Loop de render en background
        var renderLoop = Task.Run(RenderLoop);

        // Loop de HTTP requests
        while (_running)
        {
            try
            {
                var ctx = _listener.GetContext();
                _ = Task.Run(() => HandleRequest(ctx));
            }
            catch (HttpListenerException) { break; }
        }
    }

    public override void RequestRedraw() => _dirty = true;

    // ─── Render Loop ───────────────────────────────────────────────────────────

    private async Task RenderLoop()
    {
        while (_running)
        {
            if (_dirty)
            {
                _dirty = false;
                var png = RenderToPng();
                await BroadcastFrame(png);
            }
            await Task.Delay(16); // ~60 fps
        }
    }

    private byte[] RenderToPng()
    {
        // Asegurarse de tener dimensiones mínimas válidas
        int w = Math.Max(1, _width);
        int h = Math.Max(1, _height);

        var info = new SKImageInfo(w, h, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var surface = SKSurface.Create(info);
        if (surface == null) return [];

        var renderer = new SkiaRenderer(surface.Canvas);
        RenderFrame(renderer, w, h);
        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 90);
        return data.ToArray();
    }

    private async Task BroadcastFrame(byte[] png)
    {
        if (png.Length == 0) return;

        List<WebSocket> snapshot;
        lock (_clientsLock) snapshot = [.._clients];

        var dead = new List<WebSocket>();
        foreach (var ws in snapshot)
        {
            try
            {
                if (ws.State == WebSocketState.Open)
                    await ws.SendAsync(new ArraySegment<byte>(png), WebSocketMessageType.Binary, true, CancellationToken.None);
                else
                    dead.Add(ws);
            }
            catch { dead.Add(ws); }
        }
        lock (_clientsLock) dead.ForEach(ws => _clients.Remove(ws));
    }

    // ─── HTTP Request Handling ──────────────────────────────────────────────────

    private async Task HandleRequest(HttpListenerContext ctx)
    {
        if (ctx.Request.IsWebSocketRequest)
        {
            var wsCtx = await ctx.AcceptWebSocketAsync(null);
            var ws = wsCtx.WebSocket;
            lock (_clientsLock) _clients.Add(ws);

            // Enviar el primer frame de inmediato
            _dirty = true;

            await ReceiveEvents(ws);
        }
        else
        {
            var path = ctx.Request.Url?.AbsolutePath ?? "/";
            if (path == "/" || path == "/index.html")
                ServeHtml(ctx.Response);
            else
            {
                ctx.Response.StatusCode = 404;
                ctx.Response.Close();
            }
        }
    }

    private async Task ReceiveEvents(WebSocket ws)
    {
        var buffer = new byte[1024];
        while (ws.State == WebSocketState.Open)
        {
            try
            {
                var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                    break;
                }

                var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
                HandleEvent(json);
            }
            catch { break; }
        }

        lock (_clientsLock) _clients.Remove(ws);
    }

    private void HandleEvent(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var type = root.GetProperty("type").GetString();

            if (type == "resize")
            {
                _width = root.GetProperty("w").GetInt32();
                _height = root.GetProperty("h").GetInt32();
                _dirty = true;
                return;
            }

            var x = (float)root.GetProperty("x").GetDouble();
            var y = (float)root.GetProperty("y").GetDouble();

            var evtType = type switch
            {
                "down"  => PointerEventType.Pressed,
                "up"    => PointerEventType.Released,
                "move"  => PointerEventType.Moved,
                _       => PointerEventType.Released
            };

            if (evtType == PointerEventType.Pressed)
                TextFieldRenderNode.ClearFocus();

            var renderTree = GetCurrentRenderTree(_width, _height);
            renderTree.HandlePointerEvent(new PointerEvent(x, y, evtType));
            _dirty = true;
        }
        catch { }
    }

    private void ServeHtml(HttpListenerResponse response)
    {
        var html = BuildHtml();
        var bytes = Encoding.UTF8.GetBytes(html);
        response.ContentType = "text/html; charset=utf-8";
        response.ContentLength64 = bytes.Length;
        response.OutputStream.Write(bytes);
        response.Close();
    }

    private string BuildHtml() => $$"""
        <!DOCTYPE html>
        <html lang="es">
        <head>
          <meta charset="UTF-8">
          <meta name="viewport" content="width=device-width, initial-scale=1.0, user-scalable=no">
          <title>Evi App — Web</title>
          <style>
            * { margin: 0; padding: 0; box-sizing: border-box; }
            body, html {
              background: #000;
              width: 100%;
              height: 100%;
              overflow: hidden;
            }
            canvas {
              display: block;
              width: 100vw;
              height: 100vh;
              cursor: default;
              image-rendering: pixelated;
            }
            .status {
              position: fixed;
              bottom: 10px;
              right: 10px;
              font-size: 10px;
              color: rgba(255,255,255,0.3);
              pointer-events: none;
              font-family: monospace;
            }
          </style>
        </head>
        <body>
          <canvas id="c"></canvas>
          <div class="status">CONECTANDO...</div>
          <script>
            const canvas = document.getElementById('c');
            const ctx = canvas.getContext('2d', { alpha: false });
            const status = document.querySelector('.status');
            const ws = new WebSocket(`ws://${location.host}/`);
            ws.binaryType = 'blob';

            function resize() {
              canvas.width = window.innerWidth;
              canvas.height = window.innerHeight;
              if (ws.readyState === WebSocket.OPEN) {
                ws.send(JSON.stringify({ type: 'resize', w: canvas.width, h: canvas.height }));
              }
            }

            window.addEventListener('resize', resize);

            ws.onmessage = e => {
              const url = URL.createObjectURL(e.data);
              const img = new Image();
              img.onload = () => { 
                ctx.drawImage(img, 0, 0); 
                URL.revokeObjectURL(url); 
              };
              img.src = url;
            };

            ws.onopen  = () => { 
              status.textContent = 'EVI LIVE'; 
              resize();
            };
            ws.onclose = () => { status.textContent = 'OFFLINE'; };

            function getPos(e) {
              const r = canvas.getBoundingClientRect();
              const src = e.touches ? e.touches[0] : e;
              return { x: src.clientX - r.left, y: src.clientY - r.top };
            }

            function send(type, e) {
              if (ws.readyState !== WebSocket.OPEN) return;
              const {x, y} = getPos(e);
              ws.send(JSON.stringify({ type, x, y }));
              if (type !== 'move') e.preventDefault();
            }

            canvas.addEventListener('mousedown',  e => send('down', e));
            canvas.addEventListener('mouseup',    e => send('up',   e));
            canvas.addEventListener('mousemove',  e => send('move', e));
            canvas.addEventListener('touchstart', e => send('down', e), { passive: false });
            canvas.addEventListener('touchend',   e => { 
                const p = getPos(e.changedTouches ? {touches: e.changedTouches} : e); 
                if(ws.readyState===WebSocket.OPEN) ws.send(JSON.stringify({type:'up', x:p.x, y:p.y})); 
                e.preventDefault(); 
            }, { passive: false });
            canvas.addEventListener('touchmove',  e => send('move', e), { passive: false });
          </script>
        </body>
        </html>
        """;

    private static void OpenBrowser(string url)
    {
        try { System.Diagnostics.Process.Start("open", url); } catch { }
    }

    public override void HotReload(Component newRoot)
    {
        Root = newRoot;
        _dirty = true;
    }

    public void Dispose()
    {
        _running = false;
        _listener.Stop();
    }
}
#endif
