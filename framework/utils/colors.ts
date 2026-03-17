export function parseColor(color?: string, fallback: number = 0xff000000): number {
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
