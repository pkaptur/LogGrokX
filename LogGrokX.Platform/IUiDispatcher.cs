namespace LogGrokX.Platform;

public enum UiTimerPriority { Normal, Background }

public interface IUiDispatcher
{
    bool CheckAccess();
    IDisposable Post(Action action);
    IUiTimer CreateTimer(TimeSpan interval, Action tick, UiTimerPriority priority = UiTimerPriority.Normal);
}

public interface IUiTimer : IDisposable
{
    TimeSpan Interval { get; set; }
    bool IsEnabled { get; }
    void Start();
    void Stop();
}
