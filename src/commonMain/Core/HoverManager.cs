namespace Evi
{
    public static class HoverManager
    {
        public static object? HoveredIdentity { get; set; }

        public static bool IsHovered(object? identity)
        {
            return identity != null && HoveredIdentity == identity;
        }
    }
}
