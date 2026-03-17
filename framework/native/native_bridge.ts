declare function drawRect(x: number, y: number, w: number, h: number, color?: number): void;
declare function drawText(text: string, x: number, y: number, color?: number): void;
declare function setBackgroundColor(color: number): void;
declare function setTextColor(color: number): void;

export function drawRectNative(
  x: number,
  y: number,
  w: number,
  h: number,
  color?: number,
): void {
  drawRect(x, y, w, h, color);
}

export function drawTextNative(
  text: string,
  x: number,
  y: number,
  color?: number,
): void {
  drawText(text, x, y, color);
}

export function setBackgroundColorNative(color: number): void {
  setBackgroundColor(color);
}

export function setTextColorNative(color: number): void {
  setTextColor(color);
}
