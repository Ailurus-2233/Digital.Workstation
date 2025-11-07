using Serilog;
using Serilog.Events;

namespace DigitalWorkstation.Common;

public class Logger
{
    #region Singleton

    private static readonly Lazy<Logger> SingleInstance = new(() => new Logger());

    private readonly ILogger _logger;

    private static Logger Instance => SingleInstance.Value;

    private Logger()
    {
        // 初始化 ILogger
        _logger = new LoggerConfiguration().MinimumLevel.Information() // 设置最小日志级别
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning) // 过滤第三方日志
            .Enrich.FromLogContext() // 自动捕获上下文信息
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();
#if DEBUG
        // 调试模式下使用更详细的日志级别
        _logger = new LoggerConfiguration().MinimumLevel.Verbose() // 设置最小日志级别
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning) // 过滤第三方日志
            .Enrich.FromLogContext() // 自动捕获上下文信息
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();
#endif

    }

    #endregion

    #region Public API

    private static ILogger Log => Instance._logger;

    /// <summary>
    ///     发布 Verbose 级别的日志
    /// </summary>
    /// <param name="message">
    ///     日志内容
    /// </param>
    /// <param name="sender">
    ///     日志发送者
    /// </param>
    public static void Verbose(string message, string sender = "")
    {
        Log.Verbose("[{Sender}] {Message}", sender, message);
    }

    /// <summary>
    ///     发布 Debug 级别的日志
    /// </summary>
    /// <param name="message">
    ///     日志内容
    /// </param>
    /// <param name="sender">
    ///     日志发送者
    /// </param>
    public static void Debug(string message, string sender = "")
    {
        Log.Debug("[{Sender}] {Message}", sender, message);
    }

    /// <summary>
    ///     发布 Information 级别的日志
    /// </summary>
    /// <param name="message">
    ///     日志内容
    /// </param>
    /// <param name="sender">
    ///     日志发送者
    /// </param>
    public static void Information(string message, string sender = "")
    {
        Log.Information("[{Sender}] {Message}", sender, message);
    }

    /// <summary>
    ///     发布 Warning 级别的日志
    /// </summary>
    /// <param name="message">
    ///     日志内容
    /// </param>
    /// <param name="sender">
    ///     日志发送者
    /// </param>
    public static void Warning(string message, string sender = "")
    {
        Log.Warning("[{Sender}] {Message}", sender, message);
    }

    /// <summary>
    ///     发布 Error 级别的日志
    /// </summary>
    /// <param name="message">
    ///     日志内容
    /// </param>
    /// <param name="sender">
    ///     日志发送者
    /// </param>
    public static void Error(string message, string sender = "")
    {
        Log.Error("[{Sender}] {Message}", sender, message);
    }

    /// <summary>
    ///     发布 Error 级别的日志
    /// </summary>
    /// <param name="exception">
    ///     异常信息
    /// </param>
    /// <param name="message">
    ///     日志内容
    /// </param>
    /// <param name="sender">
    ///     日志发送者
    /// </param>
    public static void Error(Exception? exception, string message, string sender = "")
    {
        Log.Error(exception, "[{Sender}] {Message}", sender, message);
    }

    /// <summary>
    ///     发布 Fatal 级别的日志
    /// </summary>
    /// <param name="message">
    ///     日志内容
    /// </param>
    /// <param name="sender">
    ///     日志发送者
    /// </param>
    public static void Fatal(string message, string sender = "")
    {
        Log.Fatal("[{Sender}] {Message}", sender, message);
    }

    /// <summary>
    ///     发布 Fatal 级别的日志
    /// </summary>
    /// <param name="exception">
    ///     异常信息
    /// </param>
    /// <param name="message">
    ///     日志内容
    /// </param>
    /// <param name="sender">
    ///     日志发送者
    /// </param>
    public static void Fatal(Exception? exception, string message, string sender = "")
    {
        Log.Fatal(exception, "[{Sender}] {Message}", sender, message);
    }

    #endregion
}