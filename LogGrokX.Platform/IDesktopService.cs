namespace LogGrokX.Platform;

public interface IDesktopService
{
    void OpenUrl(string url);
    void OpenPath(string path);
    void RevealFile(string path);
    void SetClipboardText(string text);
}
