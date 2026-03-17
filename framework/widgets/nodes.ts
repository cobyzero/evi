import { Widget } from "../core/widget.js";

export type ContainerProps = {
  width?: number;
  height?: number;
  color?: string;
};

export class TextNode extends Widget {
  constructor(public text: string, public color?: string) {
    super();
  }
}

export class ContainerNode extends Widget {
  constructor(public props: ContainerProps) {
    super();
  }
}

export class ColumnNode extends Widget {
  constructor(public children: Widget[]) {
    super();
  }
}

export class RowNode extends Widget {
  constructor(public children: Widget[]) {
    super();
  }
}
