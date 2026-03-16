export abstract class Widget {
  // Base widget class
}

export abstract class StatelessWidget extends Widget {
  abstract build(): Widget;
}

class _Text extends Widget {
  constructor(public text: string) {
    super();
  }
  build() {
    return null;
  }
}

class _Container extends Widget {
  constructor(public props: { width?: number; height?: number; color?: string }) {
    super();
  }
  build() {
    return null;
  }
}

class _Column extends Widget {
  constructor(public children: Widget[]) {
    super();
  }
  build() {
    return null;
  }
}

declare function drawRect(x: number, y: number, w: number, h: number): void;
declare function drawText(text: string, x: number, y: number): void;

export function Text(text: string) { return new _Text(text); }
export function Container(props: { width?: number; height?: number; color?: string }) { return new _Container(props); }
export function Column(children: Widget[]) { return new _Column(children); }

export function runApp(widget: Widget) {
  render(widget, 0, 0);
}

function render(widget: Widget, x: number, y: number) {
  if (widget instanceof _Text) {
    drawText(widget.text, x, y);
  } else if (widget instanceof _Container) {
    drawRect(x, y, widget.props.width || 0, widget.props.height || 0);
  } else if (widget instanceof _Column) {
    let currentY = y;
    for (const child of widget.children) {
      render(child, x, currentY);
      currentY += 30; // Basic layout offset
    }
  } else if (widget instanceof StatelessWidget) {
    const built = widget.build();
    render(built, x, y);
  }
}
