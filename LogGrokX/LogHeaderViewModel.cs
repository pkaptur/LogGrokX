namespace LogGrokX
{
    public class LogHeaderViewModel : ItemViewModel
    {
        public LinePartViewModel Text { get; }

        public  LogHeaderViewModel(string text, DisplaySettingsMonitor displaySettings) => Text = new LinePartViewModel(-1, text, displaySettings);

        public override string ToString() => Text.OriginalText;
    }
}