using System;
using System.Globalization;
using System.Windows.Data;

namespace LogGrokX.Theming;

public sealed class DockThemeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        DockThemes.GetTheme(value is true);

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
