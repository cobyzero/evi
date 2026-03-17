import { Widget } from "../core/widget.js";
import {
  setBackgroundColorNative,
  setTextColorNative,
} from "../native/native_bridge.js";
import { renderWidget } from "../render/render.js";
import { parseColor } from "../utils/colors.js";

export type RunAppOptions = {
  backgroundColor?: string;
  textColor?: string;
};

export function runApp(widget: Widget, options?: RunAppOptions): void {
  setBackgroundColorNative(parseColor(options?.backgroundColor, 0xffffffff));
  setTextColorNative(parseColor(options?.textColor, 0xff000000));
  renderWidget(widget, 0, 0);
}
