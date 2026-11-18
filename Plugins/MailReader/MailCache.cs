using System.Text.Json;

namespace DigitalWorkstation.MailReader;

internal interface IMailCache
{
    Task<IReadOnlyList<MailMessage>> ReadAsync(string key, CancellationToken token);
    Task WriteAsync(string key, IReadOnlyList<MailMessage> messages, CancellationToken token);
}

// One complete snapshot per connection identity. No attachment destinations or credentials
// enter this store, so replacing/evicting a snapshot cannot delete an exported attachment.
internal sealed class MailCache(string directory) : IMailCache
{
    private sealed record Snapshot(int Version, string Key, MailMessage[] Messages);
    public static string DefaultDirectory => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Digital.Workstation", "mail-cache");

    private string CachePath(string key)
    {
        if (key.Length != 64 || key.Any(c => !Uri.IsHexDigit(c))) throw new ArgumentException("Invalid cache identity.", nameof(key));
        return Path.Combine(directory, key + ".json");
    }
    public async Task<IReadOnlyList<MailMessage>> ReadAsync(string key, CancellationToken token)
    {
        var path = CachePath(key);
        FileStream opened;
        try { opened = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete, 81920, true); }
        catch (FileNotFoundException) { return []; }
        catch (DirectoryNotFoundException) { return []; }
        await using var stream = opened;
        if (stream.Length > 512L * 1024 * 1024) throw new InvalidDataException("Cache exceeds size limit.");
        var snapshot = await JsonSerializer.DeserializeAsync<Snapshot>(stream, cancellationToken: token);
        if (snapshot is null || snapshot.Version != 1 || snapshot.Key != key || snapshot.Messages is null
            || snapshot.Messages.Length > 100 || snapshot.Messages.Any(m => !IsValid(m, key))
            || snapshot.Messages.Select(m => m.Id).Distinct(StringComparer.Ordinal).Count() != snapshot.Messages.Length)
            throw new InvalidDataException("Invalid cache snapshot.");
        return snapshot.Messages.OrderByDescending(m => m.Date).ToArray();
    }
    private static bool IsValid(MailMessage? message, string key)
    {
        if (message is null || message.Id is null || !message.Id.StartsWith(key + ":", StringComparison.Ordinal)
            || message.Subject is null || message.From is null || message.To is null || message.Body is null) return false;
        var content = message.Content;
        return content is null || (content.Blocks is not null && content.Images is not null && content.Attachments is not null
            && content.Blocks.All(b => b is not null && b.Kind is not null && b.Runs is not null && b.Runs.All(r => r is not null && r.Text is not null))
            && content.Images.All(i => i is not null && i.Id is not null && i.MediaType is not null && i.Data is not null)
            && content.Attachments.All(a => a is not null && a.Name is not null && a.MediaType is not null && a.Data is null));
    }
    private static MailImage[] InlineImages(MailContent content)
    {
        var referenced = content.Blocks.Where(block => block.Kind == "image" && block.ImageId is not null)
            .Select(block => block.ImageId).ToHashSet(StringComparer.Ordinal);
        return content.Images.Where(image => referenced.Contains(image.Id))
            .GroupBy(image => image.Id, StringComparer.Ordinal)
            .Where(group => group.Count() == 1 && MailImageDecoder.Supports(group.First().MediaType))
            .Select(group => group.First()).ToArray();
    }
    public async Task WriteAsync(string key, IReadOnlyList<MailMessage> messages, CancellationToken token)
    {
        var path = CachePath(key);
        // The protocol fetch has already selected the newest server positions. Do not select
        // another date-based window here: message dates can disagree with server order.
        var cached = messages.DistinctBy(m => m.Id, StringComparer.Ordinal).Take(100).Select(m => m with
        {
            Content = m.Content is not { } c ? null : new MailContent(c.Blocks, InlineImages(c),
                c.Attachments.Select(a => a with { CachedSize = a.Size, Data = null }).ToArray())
        }).ToArray();
        if (cached.Any(m => !IsValid(m, key))) throw new InvalidDataException("Invalid message snapshot.");
        token.ThrowIfCancellationRequested();
        Directory.CreateDirectory(directory);
        var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true))
            {
                await JsonSerializer.SerializeAsync(stream, new Snapshot(1, key, cached), cancellationToken: token);
                await stream.FlushAsync(token);
                if (stream.Length > 512L * 1024 * 1024) throw new InvalidDataException("Cache exceeds size limit.");
                stream.Flush(flushToDisk: true);
            }
            token.ThrowIfCancellationRequested();
            File.Move(temporary, path, overwrite: true);
        }
        finally
        {
            try { File.Delete(temporary); } catch (IOException) { } catch (UnauthorizedAccessException) { }
        }
    }
}
