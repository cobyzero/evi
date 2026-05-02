namespace Evi
{
    public enum KeyEventType
    {
        Pressed,
        Released,
        Character
    }

    public record KeyEvent(string Key, KeyEventType Type);
}
