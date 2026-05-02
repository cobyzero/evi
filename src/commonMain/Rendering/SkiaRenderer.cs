using System;
using SkiaSharp;

namespace Evi
{
    public class SkiaRenderer(SKCanvas canvas) : IRenderer
    {
        private readonly SKCanvas _canvas = canvas;

        public void Clear(Color color)
        {
            _canvas.Clear(ToSKColor(color));
        }

        public void DrawRect(float x, float y, float width, float height, Color color, BoxShadow? shadow = null, float borderWidth = 0, Color? borderColor = null)
        {
            using SKPaint paint = new()
            {
                Color = ToSKColor(color),
                Style = SKPaintStyle.Fill,
                IsAntialias = true
            };

            if (shadow != null)
            {
                paint.ImageFilter = SKImageFilter.CreateDropShadow(
                    shadow.OffsetX,
                    shadow.OffsetY,
                    shadow.Blur,
                    shadow.Blur,
                    ToSKColor(shadow.Color));
            }

            _canvas.DrawRect(x, y, width, height, paint);

            if (borderWidth > 0 && borderColor != null)
            {
                using SKPaint borderPaint = new()
                {
                    Color = ToSKColor(borderColor.Value),
                    Style = SKPaintStyle.Stroke,
                    StrokeWidth = borderWidth,
                    IsAntialias = true
                };
                _canvas.DrawRect(x, y, width, height, borderPaint);
            }
        }

        public void DrawRoundRect(float x, float y, float width, float height, float radius, Color color, BoxShadow? shadow = null, float borderWidth = 0, Color? borderColor = null)
        {
            using SKPaint paint = new()
            {
                Color = ToSKColor(color),
                Style = SKPaintStyle.Fill,
                IsAntialias = true
            };

            if (shadow != null)
            {
                paint.ImageFilter = SKImageFilter.CreateDropShadow(
                    shadow.OffsetX,
                    shadow.OffsetY,
                    shadow.Blur,
                    shadow.Blur,
                    ToSKColor(shadow.Color));
            }

            _canvas.DrawRoundRect(x, y, width, height, radius, radius, paint);

            if (borderWidth > 0 && borderColor != null)
            {
                using SKPaint borderPaint = new()
                {
                    Color = ToSKColor(borderColor.Value),
                    Style = SKPaintStyle.Stroke,
                    StrokeWidth = borderWidth,
                    IsAntialias = true
                };
                _canvas.DrawRoundRect(x, y, width, height, radius, radius, borderPaint);
            }
        }

        [Obsolete]
        public void DrawText(string text, float x, float y, float fontSize, Color color)
        {
            using SKPaint paint = new()
            {
                Color = ToSKColor(color),
                TextSize = fontSize,
                IsAntialias = true
            };
            _canvas.DrawText(text, x, y, paint);
        }

        public void Save()
        {
            _ = _canvas.Save();
        }

        public void Restore()
        {
            _canvas.Restore();
        }

        public void Translate(float dx, float dy)
        {
            _canvas.Translate(dx, dy);
        }

        private static SKColor ToSKColor(Color color)
        {
            return new(color.R, color.G, color.B, color.A);
        }
    }
}
