using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace LogGrokX.Controls.GridView;

public sealed class LogGridViewConverter : IValueConverter
{
    private static readonly ConditionalWeakTable<Dispatcher, DataTemplate> SourceStripeTemplates = new();
    private static readonly ColorBrushConverter BrushConverter = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not LogGridModel model) return DependencyProperty.UnsetValue;
        return GridViewFactory.CreateView(model, model.ColumnSettings.ColumnWidths,
            model.HasSourceColumn ? SourceStripeTemplates.GetValue(Dispatcher.CurrentDispatcher, _ => CreateSourceStripeTemplate()) : null,
            model.HasSourceColumn ? "Source.Document.FoldingState" : null);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();

    private static DataTemplate CreateSourceStripeTemplate()
    {
        var border = new FrameworkElementFactory(typeof(Border));
        border.SetValue(FrameworkElement.WidthProperty, 12.0);
        border.SetValue(FrameworkElement.HeightProperty, 14.0);
        border.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        border.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        border.SetValue(Border.CornerRadiusProperty, new CornerRadius(2));
        border.SetBinding(Border.BackgroundProperty, new Binding("Source.TimelineColor") { Converter = BrushConverter });
        border.SetBinding(FrameworkElement.ToolTipProperty, new Binding("SourceTitle"));
        return new DataTemplate(typeof(DependencyObject)) { VisualTree = border };
    }
}
