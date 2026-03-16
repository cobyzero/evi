export class Widget {
}
export class StatelessWidget extends Widget {
}
class _Text extends Widget {
    constructor(text) {
        super();
        this.text = text;
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
export function Text(text) { return new _Text(text); }
export function Container(props) { return new _Container(props); }
export function Column(children) { return new _Column(children); }
export function runApp(widget) {
    render(widget, 0, 0);
}
function render(widget, x, y) {
    if (widget instanceof _Text) {
        drawText(widget.text, x, y);
    }
    else if (widget instanceof _Container) {
        drawRect(x, y, widget.props.width || 0, widget.props.height || 0);
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
