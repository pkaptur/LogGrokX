using System.Windows.Media;
using AvalonDock.Themes;

namespace LogGrokX.Theming;

internal static class DockThemes
{
    private static readonly Vs2013LightTheme Light = new();
    private static readonly Vs2013DarkTheme Dark = new();

    internal static Theme GetTheme(bool isDark) => isDark ? Dark : Light;

    internal static void ApplyAccentColor()
    {
        DockThemeResources.ApplyAccentColor(Light.ThemeResourceDictionary);
        DockThemeResources.ApplyAccentColor(Dark.ThemeResourceDictionary);
    }

    internal static void ApplyAccentColor(Color color)
    {
        DockThemeResources.ApplyAccentColor(Light.ThemeResourceDictionary, color);
        DockThemeResources.ApplyAccentColor(Dark.ThemeResourceDictionary, color);
    }
}
