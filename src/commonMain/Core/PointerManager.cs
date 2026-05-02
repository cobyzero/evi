namespace Evi
{
    public static class PointerManager
    {
        public static object? PressedIdentity { get; set; }

        public static bool IsPressed(object? identity)
        {
            return identity != null && PressedIdentity == identity;
        }
    }
}
