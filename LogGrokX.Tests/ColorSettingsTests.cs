using System.Text.RegularExpressions;
using LogGrokX.Platform.Colors;
using System.Windows.Media;
using LogGrokX.Colors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MediaColors = System.Windows.Media.Colors;

namespace LogGrokX.Tests
{
    [TestClass]
    public class ColorSettingsTests
    {
        private static double Luminance(Color color) =>
            (0.2126 * color.R + 0.7152 * color.G + 0.0722 * color.B) / 255.0;

        private static Color GetColor(Brush brush)
        {
            Assert.IsNotNull(brush);
            return ((SolidColorBrush)brush).Color;
        }

        [TestMethod]
        public void LightThemeKeepsConfiguredColors()
        {
            var rule = new ColorRuleDefinition(new Regex("IMP"), MediaColors.DarkRed.ToString(), MediaColors.White.ToString());

            Assert.AreEqual(MediaColors.DarkRed, GetColor(ColorBrushes.GetForegroundBrush(rule, false)));
            Assert.AreEqual(MediaColors.White, GetColor(ColorBrushes.GetBackgroundBrush(rule, false)));
        }

        [TestMethod]
        public void DarkThemeLightensDarkForeground()
        {
            var rule = new ColorRuleDefinition(new Regex("IMP"), MediaColors.DarkRed.ToString(), string.Empty);

            var dark = GetColor(ColorBrushes.GetForegroundBrush(rule, true));

            Assert.IsTrue(Luminance(dark) > Luminance(MediaColors.DarkRed));
            Assert.IsTrue(Luminance(dark) >= 0.5);
        }

        [TestMethod]
        public void DarkThemeKeepsReadableForeground()
        {
            var rule = new ColorRuleDefinition(new Regex("DBG"), MediaColors.Gray.ToString(), string.Empty);

            Assert.AreEqual(MediaColors.Gray, GetColor(ColorBrushes.GetForegroundBrush(rule, true)));
        }

        [TestMethod]
        public void DarkThemeDarkensLightBackground()
        {
            var rule = new ColorRuleDefinition(new Regex("fatal"), string.Empty, MediaColors.White.ToString());

            var dark = GetColor(ColorBrushes.GetBackgroundBrush(rule, true));

            Assert.IsTrue(Luminance(dark) < Luminance(MediaColors.White));
            Assert.IsTrue(Luminance(dark) <= 0.3);
        }

        [TestMethod]
        public void InvalidProfileColorIsRejectedBeforeRendering()
        {
            var configuration = new Platform.Colors.Configuration.ColorSettings
            {
                Rules = [new Platform.Colors.Configuration.ColorRule { RegexString = ".*", ForegroundColor = "invalid-color" }]
            };
            Assert.ThrowsExactly<System.FormatException>(() => new ColorRuleSet(configuration, new Platform.WpfColorValidator()));
        }

        [TestMethod]
        public void MalformedColorCannotThrowFromRowRendering()
        {
            var rule = new ColorRuleDefinition(new Regex(".*"), "invalid-color", "invalid-color");
            Assert.IsNull(ColorBrushes.GetForegroundBrush(rule, false));
            Assert.IsNull(ColorBrushes.GetBackgroundBrush(rule, true));
        }
    }
}
