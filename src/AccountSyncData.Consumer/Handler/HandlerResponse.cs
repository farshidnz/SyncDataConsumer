namespace AccountSyncData.Consumer.Handler;

public record HandlerResponse(ResponseStatus Status, string? Message);

public enum ResponseStatus
{
    Failed,
    Successful
}