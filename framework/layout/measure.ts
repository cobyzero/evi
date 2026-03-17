import { StatelessWidget, Widget } from "../core/widget.js";
import { ColumnNode, ContainerNode, RowNode, TextNode } from "../widgets/nodes.js";

export function measureHeight(widget: Widget): number {
  if (widget instanceof TextNode) {
    return 30;
  }

  if (widget instanceof ContainerNode) {
    return widget.props.height || 0;
  }

  if (widget instanceof ColumnNode) {
    let total = 0;
    for (const child of widget.children) {
      total += measureHeight(child);
    }
    return total;
  }

  if (widget instanceof RowNode) {
    let maxHeight = 0;
    for (const child of widget.children) {
      maxHeight = Math.max(maxHeight, measureHeight(child));
    }
    return maxHeight;
  }

  if (widget instanceof StatelessWidget) {
    return measureHeight(widget.build());
  }

  return 0;
}

export function measureWidth(widget: Widget): number {
  if (widget instanceof TextNode) {
    // Approximate text width for simple flow layout.
    return widget.text.length * 12;
  }

  if (widget instanceof ContainerNode) {
    return widget.props.width || 0;
  }

  if (widget instanceof ColumnNode) {
    let maxWidth = 0;
    for (const child of widget.children) {
      maxWidth = Math.max(maxWidth, measureWidth(child));
    }
    return maxWidth;
  }

  if (widget instanceof RowNode) {
    let total = 0;
    for (const child of widget.children) {
      total += measureWidth(child);
    }
    return total;
  }

  if (widget instanceof StatelessWidget) {
    return measureWidth(widget.build());
  }

  return 0;
}
