using DigitalWorkstation.Core.Abstractions.Settings;
using DigitalWorkstation.MailReader.Resources;
using System.Security.Cryptography;
using System.Text;

namespace DigitalWorkstation.MailReader;

public enum MailTls { SslOnConnect, StartTls }

[SettingGroup("mail-reader", typeof(MailResources), nameof(MailResources.PluginName))]
public static class MailSettings
{
    [SettingItem("mail-reader", typeof(MailResources), "Server", Id = "mail-reader.server", DefaultValue = "", Order = 0)]
    public static string Server => "";
    [SettingItem("mail-reader", typeof(MailResources), "Port", Id = "mail-reader.port", DefaultValue = "993", Order = 1)]
    public static string Port => "993";
    [SettingItem("mail-reader", typeof(MailResources), "Tls", Id = "mail-reader.tls", DefaultValue = MailTls.SslOnConnect, Order = 2)]
    public static MailTls Tls => MailTls.SslOnConnect;
    [SettingItem("mail-reader", typeof(MailResources), "Username", Id = "mail-reader.username", DefaultValue = "", Order = 3)]
    public static string Username => "";

    public static MailAccount Read(ISettingsService settings) => new(
        (settings.Get<string>("mail-reader.server") ?? "").Trim(),
        settings.Get<string>("mail-reader.port") ?? "",
        settings.Get<MailTls>("mail-reader.tls"),
        (settings.Get<string>("mail-reader.username") ?? "").Trim());
}

public sealed record MailAccount(string Server, string Port, MailTls Tls, string Username)
{
    // Includes every connection setting; credentials cannot silently follow a changed server.
    public string Key => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
        System.Text.Json.JsonSerializer.Serialize(new[] { "IMAP", Server.ToLowerInvariant(), Port, Tls.ToString(), Username }))));
    public bool IsValid => !string.IsNullOrWhiteSpace(Server) && !string.IsNullOrWhiteSpace(Username)
        && int.TryParse(Port, out var port) && port is > 0 and <= 65535 && Enum.IsDefined(Tls);
}
