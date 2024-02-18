namespace AccountSyncData.Consumer.Models;

public interface IMessage
{
    public string MessageTypeName { get; }
}