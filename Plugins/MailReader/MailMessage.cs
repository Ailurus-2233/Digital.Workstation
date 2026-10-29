using System.Text;
using HtmlAgilityPack;
using MimeKit;

namespace DigitalWorkstation.MailReader;

public sealed record MailMessage(string Id, string Subject, string From, string To, DateTimeOffset Date, string Body)
{
    public MailContent? Content { get; init; }
    public string DisplayDate => Date.ToLocalTime().ToString("g");
}

internal static class MailText
{
    public static string Read(MimeMessage message)
    {
        if (!string.IsNullOrWhiteSpace(message.TextBody)) return message.TextBody;
        if (message.HtmlBody is not { } html) return "";
        var document = new HtmlDocument();
        document.LoadHtml(html);
        var builder = new StringBuilder();
        Append(document.DocumentNode, builder, false);
        return builder.ToString().Trim();
    }

    private static void Append(HtmlNode node, StringBuilder text, bool preformatted)
    {
        var name = node.Name.ToLowerInvariant();
        if (name is "script" or "style" or "head" or "template" or "noscript" or "iframe" or "object" or "svg") return;
        var style = node.GetAttributeValue("style", "").Replace(" ", "").ToLowerInvariant();
        if (node.Attributes["hidden"] is not null || style.Contains("display:none") || style.Contains("visibility:hidden")) return;
        if (node is HtmlTextNode leaf)
        {
            var decoded = HtmlEntity.DeEntitize(leaf.Text);
            text.Append(preformatted ? decoded : System.Text.RegularExpressions.Regex.Replace(decoded, @"\s+", " "));
            return;
        }
        if (name == "br") { text.AppendLine(); return; }
        if (name == "img") { text.Append('[').Append(node.GetAttributeValue("alt", Resources.MailResources.ImageOmitted)).Append(']'); return; }
        var block = name is "p" or "div" or "section" or "article" or "header" or "footer" or "h1" or "h2" or "h3" or "h4" or "h5" or "h6" or "ul" or "ol" or "li" or "blockquote" or "tr" or "pre" or "table";
        if (block && text.Length > 0 && text[^1] != '\n') text.AppendLine();
        if (name == "li") text.Append("• ");
        foreach (var child in node.ChildNodes) Append(child, text, preformatted || name == "pre");
        if (name is "td" or "th") text.Append(" | ");
        if (block) text.AppendLine();
    }
}
