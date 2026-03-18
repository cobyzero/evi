# EVI (Evolution Engine)

![EVI Logo](/Users/mac/.gemini/antigravity/brain/4bb173a1-fba8-4d6b-897f-e42cf03dd029/evi_engine_logo_1773856314160.png)

**EVI** es un motor de renderizado de alto rendimiento y una plataforma de desarrollo de aplicaciones moderna que combina la potencia nativa de **C++** y **Skia** con la flexibilidad y rapidez de **TypeScript** a través del motor **QuickJS**.

## 🚀 Características Principales

- **Renderizado Nativo con Skia**: Utiliza la misma biblioteca de gráficos 2D que impulsa a Chrome, Flutter y Android para obtener un rendimiento gráfico excepcional y una fidelidad visual perfecta.
- **Scripting en TypeScript**: Desarrolla la lógica de tu aplicación y la interfaz de usuario utilizando TypeScript, con todo el tipado y soporte moderno.
- **Hot Reloading / Fast Refresh**: Icluye un sistema nativo de monitoreo de cambios que recarga automáticamente los scripts en tiempo real sin necesidad de reiniciar la aplicación.
- **QuickJS Integrado**: Utiliza un motor JavaScript extremadamente ligero y rápido, diseñado específicamente para ser embebido en aplicaciones nativas.
- **Arquitectura Basada en Widgets**: Un framework de UI inspirado en paradigmas modernos (como Flutter/React) enfocado en la composición de componentes.

## 🏗️ Arquitectura del Proyecto

El proyecto está dividido en dos capas principales:

1.  **Native Engine (C++)**: Ubicado en `/engine`. Se encarga de la gestión de la ventana (GLFW), el contexto de renderizado (OpenGL), la integración con Skia y la ejecución del runtime de JavaScript (QuickJS).
2.  **TS Framework**: Ubicado en `/framework`. Proporciona las abstracciones de alto nivel, el sistema de widgets, el motor de layout y las utilidades para desarrollar aplicaciones.

## 🛠️ Requisitos Rápidos

- **macOS**: El motor está optimizado actualmente para el ecosistema Apple (Cocoa, Metal, QuartzCore).
- **CMake**: Para la configuración y construcción del motor nativo.
- **Node.js / NPM**: Para la compilación de TypeScript.
- **GLFW & OpenGL**: Asegúrate de tener instaladas las dependencias de desarrollo.

## 🏁 Cómo Empezar

Para compilar y ejecutar el proyecto completo (incluyendo el framework de TypeScript y el motor nativo), utiliza el script automatizado:

```bash
chmod +x run.sh
./run.sh
```

Este script realizará las siguientes acciones:
1. Compilará el código TypeScript utilizando `tsc`.
2. Configurará y construirá el motor nativo con `cmake` en la carpeta `build/`.
3. Iniciará el proceso de observación (`watch`) para hot-reload.
4. Ejecutará el binario del motor.

## 📂 Estructura de Directorios

- `engine/`: Código fuente C++ del motor nativo.
- `framework/`: Código TypeScript del marco de trabajo de UI.
- `scripts/`: Scripts de aplicación y lógica de usuario.
- `quickjs/`: Código fuente del motor JavaScript embebido.
- `skia/`: Bibliotecas de Skia y cabeceras.
- `dist/`: Salida de la compilación de TypeScript.
- `build/`: Salida de la compilación de CMake (nativo).

## 📜 Licencia

Este proyecto se distribuye bajo la licencia MIT. Consulta el archivo `LICENSE` para más detalles.

---
*Desarrollado con ❤️ para la próxima generación de aplicaciones de alto rendimiento.*
