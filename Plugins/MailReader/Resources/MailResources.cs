using DigitalWorkstation.Core.Resource;
namespace DigitalWorkstation.MailReader.Resources;
public static class MailResources
{
    public static string Account => ResourceText.Get(typeof(MailResources), nameof(Account));
    public static string AuthenticationFailed => ResourceText.Get(typeof(MailResources), nameof(AuthenticationFailed));
    public static string Cancelled => ResourceText.Get(typeof(MailResources), nameof(Cancelled));
    public static string ConfigureHint => ResourceText.Get(typeof(MailResources), nameof(ConfigureHint));
    public static string ConnectionFailed => ResourceText.Get(typeof(MailResources), nameof(ConnectionFailed));
    public static string ConnectionSettings => ResourceText.Get(typeof(MailResources), nameof(ConnectionSettings));
    public static string CredentialHint => ResourceText.Get(typeof(MailResources), nameof(CredentialHint));
    public static string CredentialSaved => ResourceText.Get(typeof(MailResources), nameof(CredentialSaved));
    public static string Date => ResourceText.Get(typeof(MailResources), nameof(Date));
    public static string Empty => ResourceText.Get(typeof(MailResources), nameof(Empty));
    public static string From => ResourceText.Get(typeof(MailResources), nameof(From));
    public static string ImageOmitted => ResourceText.Get(typeof(MailResources), nameof(ImageOmitted));
    public static string InvalidConfiguration => ResourceText.Get(typeof(MailResources), nameof(InvalidConfiguration));
    public static string Loaded => ResourceText.Get(typeof(MailResources), nameof(Loaded));
    public static string Loading => ResourceText.Get(typeof(MailResources), nameof(Loading));
    public static string NoSubject => ResourceText.Get(typeof(MailResources), nameof(NoSubject));
    public static string Password => ResourceText.Get(typeof(MailResources), nameof(Password));
    public static string PasswordRequired => ResourceText.Get(typeof(MailResources), nameof(PasswordRequired));
    public static string PluginName => ResourceText.Get(typeof(MailResources), nameof(PluginName));
    public static string Port => ResourceText.Get(typeof(MailResources), nameof(Port));
    public static string Refresh => ResourceText.Get(typeof(MailResources), nameof(Refresh));
    public static string SavePassword => ResourceText.Get(typeof(MailResources), nameof(SavePassword));
    public static string SelectHint => ResourceText.Get(typeof(MailResources), nameof(SelectHint));
    public static string Server => ResourceText.Get(typeof(MailResources), nameof(Server));
    public static string SessionOnly => ResourceText.Get(typeof(MailResources), nameof(SessionOnly));
    public static string Tls => ResourceText.Get(typeof(MailResources), nameof(Tls));
    public static string TlsFailed => ResourceText.Get(typeof(MailResources), nameof(TlsFailed));
    public static string TlsSslOnConnect => ResourceText.Get(typeof(MailResources), nameof(TlsSslOnConnect));
    public static string TlsStartTls => ResourceText.Get(typeof(MailResources), nameof(TlsStartTls));
    public static string To => ResourceText.Get(typeof(MailResources), nameof(To));
    public static string Username => ResourceText.Get(typeof(MailResources), nameof(Username));
}
