namespace AccountSyncData.Consumer.Models;

public class EventMetadata
{
    public Guid EventID { get; set; }
#nullable enable
    public string? EventSource { get; set; }

    public string? EventType { get; set; }

    public string? Domain { get; set; }
#nullable disable
    public Guid CorrelationID { get; set; }

    public DateTimeOffset RaisedDateTimeUTC { get; set; }

    public DateTimeOffset PublishedDateTimeUTC { get; set; }

    // the context is per domain entity/event type
    public ulong ContextualSequenceNumber { get; set; }
}