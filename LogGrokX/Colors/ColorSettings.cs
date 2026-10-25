using System.Windows;
using LogGrokX.Platform.Colors;

namespace LogGrokX.Colors
{
    public static class ColorSettings
    {
        public static readonly DependencyProperty ColorSettingsProperty = DependencyProperty.RegisterAttached(
            "ColorSettings",
            typeof(ColorRuleSet),
            typeof(ColorSettings),
            new FrameworkPropertyMetadata(default(ColorRuleSet), FrameworkPropertyMetadataOptions.Inherits));

        public static void SetColorSettings(DependencyObject element, ColorRuleSet value)
        {
            element.SetValue(ColorSettingsProperty, value);
        }

        public static ColorRuleSet? GetColorSettings(DependencyObject element)
        {
            return element.GetValue(ColorSettingsProperty) as ColorRuleSet;
        }

    }
}
