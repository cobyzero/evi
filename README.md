# 🛸 Evi Framework

**Evi** es un framework de UI declarativo, ligero y multiplataforma para .NET 9, diseñado para crear interfaces hermosas y fluidas utilizando **SkiaSharp**. Inspirado por la simplicidad de Flutter y la potencia de C#, Evi permite desplegar aplicaciones en Desktop, Móvil y Web desde una única base de código.

---

## 🚀 Características Principales

-   **Declarativo**: Construye interfaces mediante la composición de componentes simples y reutilizables.
-   **Multiplataforma**: Soporte nativo para **macOS**, **iOS**, **Android** y **Web** (vía WebSockets + Canvas).
-   **Hot Reload**: Observa tus cambios en tiempo real sin reiniciar la aplicación.
-   **CLI Inteligente**: Una herramienta de línea de comandos potente que gestiona el ciclo de vida del proyecto.
-   **Arquitectura "Pure Core"**: El framework principal es agnóstico; las dependencias nativas solo se cargan cuando son necesarias.

---

## 🛠️ Instalación rápida

Para empezar a usar Evi, simplemente clona este repositorio y añade el wrapper del CLI a tu path o úsalo directamente:

```bash
# Dar permisos de ejecución al CLI
chmod +x ./evi

# Crear un nuevo proyecto
./evi create mi_app
```

---

## 💻 Ejemplo de Código

Evi utiliza una sintaxis limpia y familiar para cualquier desarrollador de C#:

```csharp
public class MyApp : Component
{
    public override RenderNode Build()
    {
        return new Scaffold {
            AppBar = new AppBar { Title = "Evi App" },
            Body = new Center {
                Child = new Column {
                    Children = {
                        new Text("¡Hola desde Evi!"),
                        new Button {
                            Text = "Haz Click",
                            OnPressed = () => Console.WriteLine("¡Click!")
                        }
                    }
                }
            }
        };
    }
}
```

---

## 📖 Comandos del CLI

El comando `evi` es tu centro de control:

| Comando | Descripción |
| :--- | :--- |
| `evi create <name>` | Crea una estructura de proyecto nueva y lista para usar. |
| `evi run macos` | Ejecuta la aplicación en una ventana nativa de escritorio. |
| `evi run ios` | Despliega en el simulador de iOS (incluye selector de dispositivos). |
| `evi run android` | Despliega en un dispositivo o emulador Android (vía adb). |
| `evi run web` | Inicia un servidor local y abre la app en tu navegador favorito. |
| `evi build <platform>` | Genera binarios de producción optimizados. |
| `evi doctor` | Verifica que tu entorno tenga todo lo necesario. |

---

## 🌐 Soporte Web Único

Evi cuenta con un **Web Host** innovador que permite previsualizar tus apps en el navegador sin necesidad de WASM. Renderiza frames de Skia a 60fps y los transmite mediante WebSockets, permitiendo una interactividad fluida y un ciclo de desarrollo ultrarrápido.

---

## ⚖️ Licencia

Este proyecto está bajo la Licencia MIT. ¡Siéntete libre de explorar, modificar y contribuir!

---

<p align="center">
  Hecho con ❤️ por el equipo de Evi Framework.
</p>
