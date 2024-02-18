namespace AccountSyncData.Consumer.Models;
#nullable enable
public class Metadata
{
    public string? MessageId { get; set; }
    public string? CorrelationId { get; set; }
    public string? EventType { get; set; }
    public string? Domain { get; set; }
    public string? EventSource { get; set; }
    public string? RaisedDateTimeUTC { get; set; }
    public string? PublishedDateTimeUTC { get; set; }
    public int ContextualSequenceNumber { get; set; }
}
#nullable disable