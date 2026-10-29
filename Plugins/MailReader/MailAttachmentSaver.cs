namespace DigitalWorkstation.MailReader;

internal static class MailAttachmentSaver
{
    // Selection is the only source of a destination. CreateNew rejects races and existing names.
    public static async Task<bool> SaveAsync(MailAttachment attachment, Func<Task<string?>> selectDestination,
        CancellationToken cancellationToken = default)
    {
        if (attachment.Data is null) throw new InvalidOperationException("Attachment content is unavailable.");
        var path = await selectDestination();
        if (path is null) return false;
        await using var output = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true);
        await output.WriteAsync(attachment.Data, cancellationToken);
        return true;
    }
}
