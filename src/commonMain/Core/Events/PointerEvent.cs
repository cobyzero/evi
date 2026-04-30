namespace Evi
{
    public enum PointerEventType
    {
        Pressed,
        Released,
        Moved
    }

    public record PointerEvent(float X, float Y, PointerEventType Type);
}
