namespace Evi
{
    /// <summary>
    /// Contrato de renderizado interno del framework. No expuesto al usuario directamente.
    /// </summary>
    public interface IRenderer
    {
        void Clear(Color color);
        void DrawRect(float x, float y, float width, float height, Color color, BoxShadow? shadow = null, float borderWidth = 0, Color? borderColor = null);
        void DrawRoundRect(float x, float y, float width, float height, float radius, Color color, BoxShadow? shadow = null, float borderWidth = 0, Color? borderColor = null);
        void DrawText(string text, float x, float y, float fontSize, Color color);
        void Save();
        void Restore();
        void Translate(float dx, float dy);
    }
}
