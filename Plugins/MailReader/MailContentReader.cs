using HtmlAgilityPack;
using MimeKit;
using System.Text.RegularExpressions;
using DigitalWorkstation.MailReader.Resources;

namespace DigitalWorkstation.MailReader;

internal static class MailContentReader
{
    public static MailMessage Read(string id, MimeMessage mime)
    {
        var images = new List<MailImage>();
        var attachments = new List<MailAttachment>();
        foreach (var entity in mime.BodyParts)
        {
            if (entity is MimePart part)
            {
                if (part.IsAttachment || !string.IsNullOrWhiteSpace(part.FileName))
                    attachments.Add(new MailAttachment(SafeFileName(part.FileName), part.ContentType.MimeType, Decode(part)));
                if (!string.IsNullOrWhiteSpace(part.ContentId) && part.ContentType.MediaType.Equals("image", StringComparison.OrdinalIgnoreCase))
                    images.Add(new MailImage(part.ContentId, part.ContentType.MimeType, Decode(part)));
            }
            else if (entity is MessagePart { Message: not null } messagePart)
            {
                using var stream = new MemoryStream();
                messagePart.Message.WriteTo(stream);
                attachments.Add(new MailAttachment(SafeFileName(messagePart.ContentDisposition?.FileName ?? "message.eml"), "message/rfc822", stream.ToArray()));
            }
        }
        var blocks = string.IsNullOrWhiteSpace(mime.HtmlBody)
            ? new[] { new MailBlock("paragraph", [new MailRun(mime.TextBody ?? "")]) }
            : MailHtmlReader.Read(mime.HtmlBody);
        return new MailMessage(id, string.IsNullOrWhiteSpace(mime.Subject) ? MailResources.NoSubject : mime.Subject,
            mime.From.ToString(), mime.To.ToString(), mime.Date, MailText.Read(mime))
        { Content = new MailContent(blocks, images.ToArray(), attachments.ToArray()) };
    }

    private static byte[] Decode(MimePart part)
    {
        using var stream = new MemoryStream();
        part.Content?.DecodeTo(stream);
        return stream.ToArray();
    }

    // A suggestion only. The picker result, never the sender's path, controls the destination.
    internal static string SafeFileName(string? name)
    {
        name = (name ?? "").Replace('\\', '/').Split('/').Last();
        name = Regex.Replace(name, "[\\x00-\\x1f<>:\"/\\\\|?*]", "_").Trim().TrimEnd('.');
        if (string.IsNullOrWhiteSpace(name)) return "attachment";
        var stem = name.Split('.')[0];
        if (Regex.IsMatch(stem, "^(CON|PRN|AUX|NUL|COM[1-9]|LPT[1-9])$", RegexOptions.IgnoreCase)) name = "_" + name;
        return name.Length > 180 ? name[..180] : name;
    }
}

internal static class MailHtmlReader
{
    public static bool TryWebLink(string? value, out Uri? uri)
    {
        if (Uri.TryCreate(value, UriKind.Absolute, out uri) && (uri.Scheme == "http" || uri.Scheme == "https")) return true;
        uri = null; return false;
    }

    public static MailBlock[] Read(string html)
    {
        var document = new HtmlDocument();
        document.LoadHtml(html);
        var blocks = new List<MailBlock>();
        var runs = new List<MailRun>();
        void Flush(string kind, int level)
        {
            if (runs.Any(r => !string.IsNullOrWhiteSpace(r.Text)))
                blocks.Add(new MailBlock(kind.StartsWith("heading:") ? "heading" : kind, runs.ToArray(), level,
                    HeadingLevel: kind.StartsWith("heading:") ? int.Parse(kind[8..]) : 0));
            runs.Clear();
        }
        void Walk(HtmlNode node, string kind, int level, bool bold, bool italic, string? link, bool pre)
        {
            var tag = node.Name.ToLowerInvariant();
            if (tag == "svg")
            {
                Flush(kind, level);
                var description = string.Join(" ", node.Descendants()
                    .Where(n => n.Name is "title" or "desc").Select(n => HtmlEntity.DeEntitize(n.InnerText)));
                blocks.Add(new MailBlock("unsupported-image", [], level, Description: description));
                return;
            }
            if (tag is "script" or "style" or "head" or "template" or "iframe" or "object" or "embed") return;
            var style = node.GetAttributeValue("style", "").Replace(" ", "").ToLowerInvariant();
            if (node.Attributes["hidden"] is not null || style.Contains("display:none") || style.Contains("visibility:hidden")) return;
            if (node is HtmlTextNode text)
            {
                var decoded = HtmlEntity.DeEntitize(text.Text);
                runs.Add(new MailRun(pre ? decoded : Regex.Replace(decoded, @"\s+", " "), bold, italic, link)); return;
            }
            if (tag == "br") { runs.Add(new MailRun("\n", bold, italic, link)); return; }
            if (tag == "img")
            {
                Flush(kind, level);
                var src = HtmlEntity.DeEntitize(node.GetAttributeValue("src", ""));
                var alt = HtmlEntity.DeEntitize(node.GetAttributeValue("alt", ""));
                var cid = src.StartsWith("cid:", StringComparison.OrdinalIgnoreCase) ? src[4..].Trim('<', '>') : null;
                if (cid is not null) { try { cid = Uri.UnescapeDataString(cid); } catch (UriFormatException) { cid = null; } }
                blocks.Add(new MailBlock("image", [], level, cid, alt)); return;
            }
            var block = tag is "p" or "div" or "section" or "article" or "header" or "footer" or "h1" or "h2" or "h3" or "h4" or "h5" or "h6" or "ul" or "ol" or "li" or "blockquote" or "tr" or "pre" or "table";
            if (block) Flush(kind, level);
            var childKind = tag.StartsWith('h') && tag.Length == 2 && char.IsDigit(tag[1]) ? $"heading:{tag[1]}" : tag == "blockquote" ? "quote" : kind;
            var childLevel = tag is "ul" or "ol" or "blockquote" ? level + 1 : level;
            if (tag == "li")
            {
                var ordered = node.ParentNode?.Name.Equals("ol", StringComparison.OrdinalIgnoreCase) == true;
                var index = (node.ParentNode?.ChildNodes.Where(n => n.Name == "li").TakeWhile(n => n != node).Count() ?? 0) + 1;
                runs.Add(new MailRun(ordered ? $"{index}. " : "• "));
            }
            if (tag == "a") link = TryWebLink(HtmlEntity.DeEntitize(node.GetAttributeValue("href", "")), out var target) ? target!.AbsoluteUri : null;
            foreach (var child in node.ChildNodes)
                Walk(child, childKind, childLevel, bold || tag is "b" or "strong" or "th" || Regex.IsMatch(style, @"font-weight:(bold(er)?|[6-9]00)(;|$)"), italic || tag is "i" or "em" || Regex.IsMatch(style, @"font-style:(italic|oblique)(;|$)"), link, pre || tag == "pre");
            if (tag is "td" or "th") runs.Add(new MailRun(" | "));
            if (block) Flush(childKind, childLevel);
        }
        Walk(document.DocumentNode, "paragraph", 0, false, false, null, false);
        Flush("paragraph", 0);
        return blocks.ToArray();
    }
}


