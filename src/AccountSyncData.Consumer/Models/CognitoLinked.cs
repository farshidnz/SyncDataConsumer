namespace AccountSyncData.Consumer.Models;

public class CognitoLinked : AccountsEventBase, IMessage
{
    public string MessageTypeName => nameof(CognitoLinked);

    public string? CognitoPoolId { get; set; }
}
