namespace AccountSyncData.Consumer.Queue;

public sealed record Message
{
    public Dictionary<string, string> Attributes = new();
    public string Body { get; init; }
    public Dictionary<string, MessageAttribute> MessageAttributes = new();
    public string MessageId { get; init; }
    public string ReceiptHandle{ get; init; }
}

public sealed record MessageAttribute
{
    private string _stringValue;
    public string StringValue
    {
        get => this._stringValue;
        set => this._stringValue = value;
    }
}