namespace LogGrokX.Platform;

public interface IThemeService
{
    bool IsDark { get; }
    string Toggle();
    void ApplyAccentColor(AccentColorKind accentColor);
}
