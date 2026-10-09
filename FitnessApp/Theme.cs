namespace FitnessApp
{
    // Shared color palette so every screen uses the same reds, greens and grays.
    // Keep these in sync with the Color resources in App.xaml.
    public static class Theme
    {
        public static readonly Color PageBackground = Color.FromArgb("#212121");
        public static readonly Color CardBackground = Color.FromArgb("#333333");
        public static readonly Color InputBackground = Color.FromArgb("#2A2A2A");
        public static readonly Color Border = Color.FromArgb("#8C8C8C");

        public static readonly Color TextPrimary = Color.FromArgb("#FFFFFF");
        public static readonly Color TextSecondary = Color.FromArgb("#D3D3D3");
        public static readonly Color TextMuted = Color.FromArgb("#8E8E93");

        public static readonly Color Accent = Color.FromArgb("#4CAF50");
        public static readonly Color Danger = Color.FromArgb("#E53935");
        public static readonly Color TagBackground = Color.FromArgb("#4A2A2A");
        public static readonly Color TagBorder = Danger;

        public static readonly Color CheckIdle = Color.FromArgb("#5A5A5A");
        public static readonly Color CheckReady = Color.FromArgb("#9E9E9E");
    }
}
