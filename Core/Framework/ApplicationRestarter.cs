using System.Diagnostics;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using DigitalWorkstation.Core.Abstractions.Settings;
using DigitalWorkstation.Core.Common;
using DigitalWorkstation.Core.Framework.Settings;

namespace DigitalWorkstation.Core.Framework;

/// <summary>
///     「立即重启」（ADR-0006 决策 7）：强制落盘在途的设置防抖保存，
///     以同一可执行文件与原始命令行参数启动新进程，随后走正常桌面生命周期关闭当前进程。
///     新进程会再次经过启动台，属预期行为
/// </summary>
public static class ApplicationRestarter
{
    public static void Restart()
    {
        // 防抖落盘有 500ms 窗口：不强制落盘，新进程可能读到不含本次修改的旧配置
        if (IoC.Provider.Resolve<ISettingsService>() is SettingsService settings)
        {
            settings.FlushPending();
        }

        var processPath = Environment.ProcessPath;
        if (processPath is null)
        {
            Logger.Error("无法确定当前进程可执行文件路径，重启中止", nameof(ApplicationRestarter));
            return;
        }

        Logger.Information("立即重启：启动新进程并退出当前进程", nameof(ApplicationRestarter));
        Process.Start(new ProcessStartInfo(processPath, Environment.GetCommandLineArgs().Skip(1)));
        (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.Shutdown();
    }
}
