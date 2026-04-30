# Evi UI Framework - Reglas de Desarrollo

Este documento define las reglas arquitectónicas y de estilo para el desarrollo de Evi.

## 1. Arquitectura de Capas
- **Core**: Debe ser 100% agnóstico a la plataforma y al motor gráfico. No puede referenciar SkiaSharp ni Silk.NET. Contiene la lógica de componentes, árbol de render, layout y eventos.
- **Rendering**: Define interfaces (`IRenderer`) para el dibujado. Las implementaciones concretas (ej. `SkiaRenderer`) pueden usar librerías externas.
- **Components**: Librería de elementos básicos. Deben construirse usando solo abstracciones de Core.
- **Host**: Punto de entrada específico por plataforma. Es el único lugar donde se permite código de ventana y entrada nativa.

## 2. Flujo de Datos
- **Unidireccional**: Los componentes generan el árbol de render (`Build()`). Los cambios de estado deben provocar un nuevo ciclo de `Build`.
- **Desacoplamiento Gráfico**: Los `RenderNode` no dibujan; llaman a métodos de `IRenderer`.
- **Propagación de Eventos**: Los eventos viajan desde el Host hacia el Core, donde se realiza un Hit-Testing para encontrar el nodo objetivo.

## 3. Estilo de Código
- Usar **File-scoped namespaces** para reducir la indentación.
- Usar **Primary Constructors** de C# 12/13 donde sea apropiado.
- Mantener los archivos pequeños y enfocados (Single Responsibility Principle).
- Comentarios en español para la lógica de negocio/educativa, pero nombres de variables y métodos en inglés (estándar de la industria).

## 4. Gestión de Estado (Futuro)
- El estado debe ser inmutable o gestionado mediante patrones claros (tipo Hooks o State objects) para evitar efectos secundarios en el árbol de render.
