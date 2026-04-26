namespace IdleGame.Utils
{
    public static class NumberFormatter
    {
        public static string Format(double value)
        {
            if (value >= 1_000_000_000_000) return $"{value / 1_000_000_000_000:F2}T";
            if (value >= 1_000_000_000) return $"{value / 1_000_000_000:F2}B";
            if (value >= 1_000_000) return $"{value / 1_000_000:F2}M";
            if (value >= 1_000) return $"{value / 1_000:F1}K";
            return $"{value:F0}";
        }
    }
}
