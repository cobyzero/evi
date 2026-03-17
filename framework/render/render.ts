import { StatelessWidget, Widget } from "../core/widget.js";
import { measureHeight, measureWidth } from "../layout/measure.js";
import { drawRectNative, drawTextNative } from "../native/native_bridge.js";
import { parseColor } from "../utils/colors.js";
import { ColumnNode, ContainerNode, RowNode, TextNode } from "../widgets/nodes.js";

export function renderWidget(widget: Widget, x: number, y: number): void {
  if (widget instanceof TextNode) {
    drawTextNative(widget.text, x, y, parseColor(widget.color, 0));
    return;
  }

  if (widget instanceof ContainerNode) {
    drawRectNative(
      x,
      y,
      widget.props.width || 0,
      widget.props.height || 0,
      parseColor(widget.props.color, 0xffff0000),
    );
    return;
  }

  if (widget instanceof ColumnNode) {
    let currentY = y;
    for (const child of widget.children) {
      renderWidget(child, x, currentY);
      currentY += measureHeight(child);
    }
    return;
  }

  if (widget instanceof RowNode) {
    let currentX = x;
    for (const child of widget.children) {
      renderWidget(child, currentX, y);
      currentX += measureWidth(child);
    }
    return;
  }

  if (widget instanceof StatelessWidget) {
    renderWidget(widget.build(), x, y);
  }
}
