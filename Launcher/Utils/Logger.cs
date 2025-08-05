using System.Text;

namespace DigitalWorkstation.Launcher.Utils;

public static class Logger
{
    private enum Level : byte
    {
        Information = 1,
        Warning,
        Error,
        Fatal
    }

    
    private static readonly ReaderWriterLockSlim _Lock = new ReaderWriterLockSlim();

    private static string BuildMessage(Level level, string sender, string? message = null, Exception? exception = null)
    {
        var sb = new StringBuilder();
        var timestamp = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}";

        var showMessage = !string.IsNullOrWhiteSpace(message);
        var showException = exception != null;

        if (!showMessage && !showException)
            throw new ArgumentException("Message or exception must be provided.");

        if (showMessage)
        {
            sb.Append($"[Launcher-{level} {timestamp}]-[{sender}] : {message}");
            if (showException)
                sb.Append(Environment.NewLine);
        }

        if (!showException)
            return sb.ToString();
        
        sb.Append($"Exception: {exception?.GetType().Name}");
        if (!string.IsNullOrEmpty(exception?.Message))
            sb.Append($": {exception.Message}");

        if (!string.IsNullOrEmpty(exception?.StackTrace))
            sb.Append($"{Environment.NewLine}{exception.StackTrace}");

        return sb.ToString();
    }

    private static void Log(Level level, string sender, string? message = null, Exception? exception = null)
    {
        _Lock.EnterReadLock();
        try
        {
            var logMessage = BuildMessage(level, sender, message, exception);
            Console.WriteLine(logMessage);
        }
        finally
        {
            _Lock.ExitReadLock();
        }
    }

    public static void Information(string sender, string message) { Log(Level.Information, sender, message); }

    public static void Warning(string sender, string message) { Log(Level.Warning, sender, message); }

    public static void Error(string sender, string? message = null, Exception? exception = null) { Log(Level.Error, sender, message, exception); }

    public static void Fatal(string sender, string? message = null, Exception? exception = null) { Log(Level.Fatal, sender, message, exception); }
}