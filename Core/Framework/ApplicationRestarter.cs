using System.Diagnostics;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using DigitalWorkstation.Core.Common;
using DigitalWorkstation.Core.Framework.Persistence;

namespace DigitalWorkstation.Core.Framework;

/// <summary>
///     「立即重启」（ADR-0006 (https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0006-attribute-settings-registration.md) 决策 7）：强制完成配置保存，
///     以同一可执行文件与原始命令行参数启动新进程，随后走正常桌面生命周期关闭当前进程。
///     新进程会再次经过启动台，属预期行为
/// </summary>
public static class ApplicationRestarter
{
    public static void Restart()
    {
        // 新进程启动前统一保存设置和布局，失败时留在当前进程以便重试。
        if (!IoC.Provider.Resolve<ConfigurationPersistence>().FlushPending())
        {
            Logger.Error("Failed to save pending configuration; restart aborted", nameof(ApplicationRestarter));
            return;
        }

        var processPath = Environment.ProcessPath;
        if (processPath is null)
        {
            Logger.Error("Unable to determine the current process executable path; restart aborted",
                nameof(ApplicationRestarter));
            return;
        }

        Logger.Information("Restarting now: starting a new process and exiting the current process",
            nameof(ApplicationRestarter));
        Process.Start(new ProcessStartInfo(processPath, Environment.GetCommandLineArgs().Skip(1)));
        (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.Shutdown();
    }
}
