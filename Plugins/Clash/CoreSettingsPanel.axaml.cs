using System.Diagnostics;
using System.Text.RegularExpressions;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace DigitalWorkstation.Clash;

public sealed partial class CoreSettingsPanel : UserControl
{
    private readonly ClashService service;
    private DateTime? inspected;
    private int revision;
    public CoreSettingsPanel() : this(DigitalWorkstation.Core.Common.IoC.Provider.Resolve<ClashService>()) { }
    public CoreSettingsPanel(ClashService service)
    {
        this.service = service;
        InitializeComponent();
        AttachedToVisualTree += (_, _) => { service.Changed += Refresh; inspected = null; Refresh(); };
        DetachedFromVisualTree += (_, _) => { service.Changed -= Refresh; revision++; };
    }

    private async void Refresh()
    {
        var path = service.CorePath;
        var file = new FileInfo(path);
        bool exists = file.Exists;
        Download.IsEnabled = !service.Busy && !exists;
        UpdateButton.IsEnabled = DeleteButton.IsEnabled = !service.Busy && exists;
        Feedback.Text = service.Operation == ClashOperation.Other ? ClashResources.Busy : service.Message;
        if (!exists)
        {
            revision++;
            inspected = null;
            VersionText.Text = ClashResources.Missing;
            return;
        }
        if (inspected == file.LastWriteTimeUtc) return;
        inspected = file.LastWriteTimeUtc;
        int current = ++revision;
        VersionText.Text = ClashResources.ReadingVersion;
        try
        {
            using var process = Process.Start(new ProcessStartInfo(path, "-v")
            {
                CreateNoWindow = true, UseShellExecute = false, RedirectStandardOutput = true
            }) ?? throw new IOException();
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            string output;
            try
            {
                output = await process.StandardOutput.ReadToEndAsync(timeout.Token);
                await process.WaitForExitAsync(timeout.Token);
            }
            catch (OperationCanceledException)
            {
                if (!process.HasExited) process.Kill();
                throw;
            }
            var match = Regex.Match(output, @"\bv\d+\.\d+\.\d+[\w.\-]*");
            if (current == revision) VersionText.Text = match.Success ? string.Format(ClashResources.Installed, match.Value) : ClashResources.UnknownVersion;
        }
        catch (Exception)
        {
            if (current == revision) VersionText.Text = ClashResources.UnreadableVersion;
        }
    }

    private async void Install(object? sender, RoutedEventArgs e) => await service.Run(service.InstallCore);
    private async void Remove(object? sender, RoutedEventArgs e) => await service.Run(() =>
    {
        service.DeleteCore();
        return Task.CompletedTask;
    });
}
