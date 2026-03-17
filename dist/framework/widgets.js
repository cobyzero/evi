export class Widget {
}
export class StatelessWidget extends Widget {
}
class _Text extends Widget {
    constructor(text, color) {
        super();
        this.text = text;
        this.color = color;
    }
    build() {
        return null;
    }
}
class _Container extends Widget {
    constructor(props) {
        super();
        this.props = props;
    }
    build() {
        return null;
    }
}
class _Column extends Widget {
    constructor(children) {
        super();
        this.children = children;
    }
    build() {
        return null;
    }
}
function parseColor(color, fallback = 0xff000000) {
    if (!color)
        return fallback;
    const named = {
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
    if (lower in named)
        return named[lower];
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
export function Text(text, color) { return new _Text(text, color); }
export function Container(props) { return new _Container(props); }
export function Column(children) { return new _Column(children); }
export function runApp(widget, options) {
    setBackgroundColor(parseColor(options === null || options === void 0 ? void 0 : options.backgroundColor, 0xffffffff));
    setTextColor(parseColor(options === null || options === void 0 ? void 0 : options.textColor, 0xff000000));
    render(widget, 0, 0);
}
function render(widget, x, y) {
    if (widget instanceof _Text) {
        drawText(widget.text, x, y, parseColor(widget.color, 0));
    }
    else if (widget instanceof _Container) {
        drawRect(x, y, widget.props.width || 0, widget.props.height || 0, parseColor(widget.props.color, 0xffff0000));
    }
    else if (widget instanceof _Column) {
        let currentY = y;
        for (const child of widget.children) {
            render(child, x, currentY);
            currentY += 30; // Basic layout offset
        }
    }
    else if (widget instanceof StatelessWidget) {
        const built = widget.build();
        render(built, x, y);
    }
}
