using System;

namespace Evi
{
    public class TextEditingController
    {
        public string Text { get; set; } = string.Empty;
        public Action<string>? OnChanged { get; set; }
    }

    public class TextField : Component
    {
        public string Placeholder { get; set; } = string.Empty;
        public TextEditingController? Controller { get; set; }
        public Action<string>? OnChanged { get; set; }
        public Color BackgroundColor { get; set; } = new Color(245, 245, 245);
        public Color TextColor { get; set; } = Color.Black;
        public float FontSize { get; set; } = 18;
        public float Width { get; set; } = 250;
        public float Height { get; set; } = 45;
        public BoxShadow? Shadow { get; set; }
        public bool IsPassword { get; set; } = false;

        public override RenderNode Build()
        {
            return new TextFieldRenderNode
            {
                Placeholder = Placeholder,
                Controller = Controller,
                ExternalOnChanged = OnChanged,
                BackgroundColor = BackgroundColor,
                TextColor = TextColor,
                FontSize = FontSize,
                Width = Width,
                Height = Height,
                Shadow = Shadow,
                IsPassword = IsPassword
            };
        }
    }

    public class TextFieldRenderNode : RenderNode
    {
        public string Placeholder { get; set; } = string.Empty;
        public TextEditingController? Controller { get; set; }
        public Action<string>? ExternalOnChanged { get; set; }
        public Color BackgroundColor { get; set; } = Color.White;
        public Color TextColor { get; set; } = Color.Black;
        public float FontSize { get; set; } = 18;
        public BoxShadow? Shadow { get; set; }
        public bool IsPassword { get; set; } = false;

        // Identidad global de foco
        internal static object? _focusedIdentity = null;

        public static void ClearFocus()
        {
            _focusedIdentity = null;
        }

        public bool IsFocused => Controller != null && _focusedIdentity == Controller;

        public override void CopyPropertiesFrom(RenderNode other)
        {
            if (other is TextFieldRenderNode otherField)
            {
                Placeholder = otherField.Placeholder;
                Controller = otherField.Controller;
                ExternalOnChanged = otherField.ExternalOnChanged;
                BackgroundColor = otherField.BackgroundColor;
                TextColor = otherField.TextColor;
                FontSize = otherField.FontSize;
                Shadow = otherField.Shadow;
                IsPassword = otherField.IsPassword;
            }
        }

        public override void Render(IRenderer renderer)
        {
            renderer.Save();
            renderer.Translate(X, Y);

            renderer.DrawRoundRect(0, 0, Width, Height, 8, BackgroundColor, Shadow);

            if (IsFocused)
            {
                // Borde de enfoque más visible
                renderer.DrawRect(0, Height - 3, Width, 3, new Color(137, 80, 255));
            }

            string text = Controller?.Text ?? string.Empty;
            string displayValue = string.IsNullOrEmpty(text) ? Placeholder : (IsPassword ? new string('●', text.Length) : text);
            Color displayColor = string.IsNullOrEmpty(text) ? new Color(180, 180, 180) : TextColor;

            float textY = (Height / 2) + (FontSize / 3);
            renderer.DrawText(displayValue, 12, textY, FontSize, displayColor);

            renderer.Restore();
        }

        public override void Layout(float maxWidth, float maxHeight)
        {
            if (Width == 0) Width = Math.Min(250, maxWidth);
            if (Height == 0) Height = Math.Min(45, maxHeight);
        }

        public override void HandlePointerEvent(PointerEvent e)
        {
            if (e.Type == PointerEventType.Pressed)
            {
                _focusedIdentity = Controller;
            }
            base.HandlePointerEvent(e);
        }

        public override void HandleKeyEvent(KeyEvent e)
        {
            if (!IsFocused || Controller == null) return;

            if (e.Type == KeyEventType.Character)
            {
                Controller.Text += e.Key;
                Controller.OnChanged?.Invoke(Controller.Text);
                ExternalOnChanged?.Invoke(Controller.Text);
            }
            else if (e.Type == KeyEventType.Pressed)
            {
                if (e.Key == "Backspace" && Controller.Text.Length > 0)
                {
                    Controller.Text = Controller.Text.Substring(0, Controller.Text.Length - 1);
                    Controller.OnChanged?.Invoke(Controller.Text);
                    ExternalOnChanged?.Invoke(Controller.Text);
                }
                if (e.Key == "Enter")
                {
                    _focusedIdentity = null;
                }
            }
        }
    }
}
