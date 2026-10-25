using System.Windows;
using System.Windows.Data;
using LogGrokX.Search;

namespace LogGrokX.Controls;

public sealed class SavedSearchCollectionViewSource : CollectionViewSource
{
    public static readonly DependencyProperty FilterTextProperty = DependencyProperty.Register(
        nameof(FilterText), typeof(string), typeof(SavedSearchCollectionViewSource),
        new PropertyMetadata(string.Empty, (source, _) => ((SavedSearchCollectionViewSource)source).View?.Refresh()));

    public string FilterText
    {
        get => (string)GetValue(FilterTextProperty);
        set => SetValue(FilterTextProperty, value);
    }

    public SavedSearchCollectionViewSource()
    {
        Filter += (_, e) => e.Accepted = e.Item is SavedSearchPattern pattern &&
            (string.IsNullOrWhiteSpace(FilterText) || pattern.MatchesFilter(FilterText));
    }
}
