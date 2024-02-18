namespace AccountSyncData.Consumer.Models;

public class MemberJoined : AccountsEventBase, IMessage
{
    public string MessageTypeName => nameof(MemberJoined);
    public int MemberId { get; set; }
    public int ClientId { get; set; }
#nullable enable
    public string? CognitoPoolId { get; set; }
#nullable disable
    public int OriginationSource { get; set; } // 1 - website, 2- mobileapp, 3 - moneyMe App
    public DateTime DateJoined { get; set; }  // It's UTC format
    public Guid MemberNewId { get; set; }
    public Boolean IsValidated { get; set; }
    public int Status { get; set; }
}