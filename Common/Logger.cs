namespace StardewMods.Common;
internal class Logger : ILogger
{
    private readonly IMonitor monitor;

    public Logger(IMonitor monitor)
    {
        this.monitor = monitor;
    }

    public void Trace(string message)
    {
        this.monitor.Log(message, LogLevel.Trace);
    }

    public void Warn(string message)
    {
        this.monitor.Log(message, LogLevel.Trace);
    }

    public void Debug(string message)
    {
        this.monitor.Log(message, LogLevel.Trace);
    }

    public void Error(string message)
    {
        this.monitor.Log(message, LogLevel.Trace);
    }

    public void Info(string message)
    {
        this.monitor.Log(message, LogLevel.Info);
    }
}
