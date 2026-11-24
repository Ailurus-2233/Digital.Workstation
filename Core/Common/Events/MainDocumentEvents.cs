namespace DigitalWorkstation.Core.Common.Events;

/// <summary>A document within a contributed view. The shell binds Model to the document's view.</summary>
public sealed record MainDocument(string ViewId, string DocumentId, string Title, object Model);

public sealed class OpenMainDocumentEvent : PubSubEvent<MainDocument>;

/// <summary>Close documents belonging to a view when their source is no longer available.</summary>
public sealed class CloseMainDocumentsEvent : PubSubEvent<string>;
