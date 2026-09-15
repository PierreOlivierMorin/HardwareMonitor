using System.Collections.Generic;
using System.Windows.Media;

namespace HardwareMonitor.Models;

public class AppTheme
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public string Icon { get; init; } = "🎨";
    public Color CpuColor { get; init; }
    public Color GpuColor { get; init; }
    public Color BackgroundColor { get; init; }
    public Color CardBackgroundColor { get; init; }
    public Color BorderColor { get; init; }
    public Color AccentColor { get; init; }

    public Brush CpuBrush => new SolidColorBrush(CpuColor);
    public Brush GpuBrush => new SolidColorBrush(GpuColor);
    public Brush BackgroundBrush => new SolidColorBrush(BackgroundColor);
    public Brush CardBackgroundBrush => new SolidColorBrush(CardBackgroundColor);
    public Brush BorderBrush => new SolidColorBrush(BorderColor);
    public Brush AccentBrush => new SolidColorBrush(AccentColor);

    public static List<AppTheme> AvailableThemes { get; } = new()
    {
        new AppTheme
        {
            Id = "cyber_dark",
            Name = "Cyber Muted (Défaut)",
            Icon = "🌌",
            CpuColor = Color.FromRgb(56, 189, 248),       // Soft Sky Blue (#38BDF8)
            GpuColor = Color.FromRgb(52, 211, 153),       // Soft Emerald (#34D399)
            BackgroundColor = Color.FromRgb(13, 17, 23),  // GitHub Dark Dim
            CardBackgroundColor = Color.FromRgb(22, 27, 34),
            BorderColor = Color.FromRgb(48, 54, 61),
            AccentColor = Color.FromRgb(56, 189, 248)
        },
        new AppTheme
        {
            Id = "nordic_frost",
            Name = "Nordic Frost",
            Icon = "❄️",
            CpuColor = Color.FromRgb(129, 140, 248),      // Soft Indigo (#818CF8)
            GpuColor = Color.FromRgb(45, 212, 191),       // Soft Teal (#2DD4BF)
            BackgroundColor = Color.FromRgb(15, 23, 42),  // Slate 900
            CardBackgroundColor = Color.FromRgb(30, 41, 59),
            BorderColor = Color.FromRgb(51, 65, 85),
            AccentColor = Color.FromRgb(129, 140, 248)
        },
        new AppTheme
        {
            Id = "synthwave",
            Name = "Synthwave Night",
            Icon = "💜",
            CpuColor = Color.FromRgb(192, 132, 252),      // Soft Lavender (#C084FC)
            GpuColor = Color.FromRgb(251, 146, 60),       // Warm Peach (#FB923C)
            BackgroundColor = Color.FromRgb(24, 17, 36),  // Deep Violet
            CardBackgroundColor = Color.FromRgb(37, 26, 56),
            BorderColor = Color.FromRgb(68, 50, 99),
            AccentColor = Color.FromRgb(192, 132, 252)
        },
        new AppTheme
        {
            Id = "stealth_black",
            Name = "OLED Stealth",
            Icon = "🌑",
            CpuColor = Color.FromRgb(148, 163, 184),      // Slate Silver (#94A3B8)
            GpuColor = Color.FromRgb(100, 116, 139),      // Cool Muted Gray (#64748B)
            BackgroundColor = Color.FromRgb(0, 0, 0),     // True Black
            CardBackgroundColor = Color.FromRgb(20, 20, 20),
            BorderColor = Color.FromRgb(40, 40, 40),
            AccentColor = Color.FromRgb(203, 213, 225)
        },
        new AppTheme
        {
            Id = "crimson_ember",
            Name = "Crimson Ember",
            Icon = "🔥",
            CpuColor = Color.FromRgb(248, 113, 113),      // Soft Coral Red (#F87171)
            GpuColor = Color.FromRgb(251, 191, 36),       // Warm Amber (#FBBF24)
            BackgroundColor = Color.FromRgb(26, 17, 20),  // Dark Ember
            CardBackgroundColor = Color.FromRgb(38, 25, 29),
            BorderColor = Color.FromRgb(72, 44, 52),
            AccentColor = Color.FromRgb(248, 113, 113)
        }
    };
}
