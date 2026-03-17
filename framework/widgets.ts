import { runApp } from "./app/run_app.js";
import { Widget } from "./core/widget.js";
import {
  ColumnNode,
  ContainerNode,
  type ContainerProps,
  RowNode,
  TextNode,
} from "./widgets/nodes.js";

export { runApp };
export { StatelessWidget, Widget } from "./core/widget.js";
export type { RunAppOptions } from "./app/run_app.js";

export function Text(text: string, color?: string): TextNode {
  return new TextNode(text, color);
}

export function Container(props: ContainerProps): ContainerNode {
  return new ContainerNode(props);
}

export function Column(children: Widget[]): ColumnNode {
  return new ColumnNode(children);
}

export function Row(children: Widget[]): RowNode {
  return new RowNode(children);
}
