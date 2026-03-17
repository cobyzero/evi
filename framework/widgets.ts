export abstract class Widget {
  // Base widget class
}

export abstract class StatelessWidget extends Widget {
  abstract build(): Widget;
}

class _Text extends Widget {
  constructor(public text: string, public color?: string) {
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

declare function drawRect(x: number, y: number, w: number, h: number, color?: number): void;
declare function drawText(text: string, x: number, y: number, color?: number): void;
declare function setBackgroundColor(color: number): void;
declare function setTextColor(color: number): void;

function parseColor(color?: string, fallback: number = 0xff000000): number {
  if (!color) return fallback;

  const named: Record<string, number> = {
    black: 0xff000000,
    white: 0xffffffff,
    red: 0xffff0000,
    green: 0xff00ff00,
    blue: 0xff0000ff,
    yellow: 0xffffff00,
    cyan: 0xff00ffff,
    magenta: 0xffff00ff,
    gray: 0xff808080,
    grey: 0xff808080,
  };

  const lower = color.toLowerCase().trim();
  if (lower in named) return named[lower];

  if (lower.startsWith("#")) {
    const hex = lower.slice(1);
    if (/^[0-9a-f]{6}$/i.test(hex)) {
      return Number.parseInt(`ff${hex}`, 16) >>> 0;
    }
    if (/^[0-9a-f]{8}$/i.test(hex)) {
      return Number.parseInt(hex, 16) >>> 0;
    }
  }

  return fallback;
}

export function Text(text: string, color?: string) { return new _Text(text, color); }
export function Container(props: { width?: number; height?: number; color?: string }) { return new _Container(props); }
export function Column(children: Widget[]) { return new _Column(children); }

export function runApp(widget: Widget, options?: { backgroundColor?: string; textColor?: string }) {
  setBackgroundColor(parseColor(options?.backgroundColor, 0xffffffff));
  setTextColor(parseColor(options?.textColor, 0xff000000));
  render(widget, 0, 0);
}

function render(widget: Widget, x: number, y: number) {
  if (widget instanceof _Text) {
    drawText(widget.text, x, y, parseColor(widget.color, 0));
  } else if (widget instanceof _Container) {
    drawRect(x, y, widget.props.width || 0, widget.props.height || 0,
             parseColor(widget.props.color, 0xffff0000));
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
