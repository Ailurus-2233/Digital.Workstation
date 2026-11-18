namespace DigitalWorkstation.MailReader;

public sealed record MailRun(string Text, bool Bold = false, bool Italic = false, string? Link = null);
public sealed record MailBlock(string Kind, MailRun[] Runs, int Level = 0, string? ImageId = null, string? Description = null, int HeadingLevel = 0);
public sealed record MailImage(string Id, string MediaType, byte[] Data);
public sealed record MailAttachment(string Name, string MediaType, byte[]? Data, long? CachedSize = null)
{
    public long? Size => Data?.LongLength ?? CachedSize;
}

// Data only: no MIME handles, controls, URLs to fetch, or executable markup are retained.
public sealed class MailContent(MailBlock[] blocks, MailImage[] images, MailAttachment[] attachments) : IEquatable<MailContent>
{
    public MailBlock[] Blocks { get; } = blocks;
    public MailImage[] Images { get; } = images;
    public MailAttachment[] Attachments { get; } = attachments;
    public bool Equals(MailContent? other) => other is not null
        && Blocks.Length == other.Blocks.Length && Blocks.Zip(other.Blocks).All(pair =>
            pair.First.Kind == pair.Second.Kind && pair.First.Level == pair.Second.Level
            && pair.First.HeadingLevel == pair.Second.HeadingLevel && pair.First.ImageId == pair.Second.ImageId
            && pair.First.Description == pair.Second.Description && pair.First.Runs.SequenceEqual(pair.Second.Runs))
        && Images.Length == other.Images.Length && Images.Zip(other.Images).All(pair =>
            pair.First.Id == pair.Second.Id && pair.First.MediaType == pair.Second.MediaType
            && pair.First.Data.AsSpan().SequenceEqual(pair.Second.Data))
        && Attachments.Length == other.Attachments.Length && Attachments.Zip(other.Attachments).All(pair =>
            pair.First.Name == pair.Second.Name && pair.First.MediaType == pair.Second.MediaType
            && pair.First.Size == pair.Second.Size
            && (pair.First.Data is null) == (pair.Second.Data is null)
            && (pair.First.Data is null || pair.First.Data.AsSpan().SequenceEqual(pair.Second.Data)));
    public override bool Equals(object? obj) => obj is MailContent other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Blocks.Length, Images.Length, Attachments.Length);
}
