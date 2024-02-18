namespace AccountSyncData.Consumer.Models;

public class MemberDetailChanged : AccountsEventBase, IMessage
{
    public string MessageTypeName =>
        nameof(MemberDetailChanged); 

    public ulong MemberId { get; set; }
    public int ClientId { get; set; }
    public string MemberNewId { get; set; }
    public bool? IsValidated { get; set; }
    public int? Status { get; set; }
    public string? Accesscode { get; set; }
    public bool? Receivenewsletter { get; set; }
    public bool? Smsconsent { get; set; }
    public bool? Appnotificationconsent { get; set; }
    public bool? Isrisky { get; set; }
    public string? Comment { get; set; }
    public bool? Active { get; set; }
    public bool? Premiumstatus { get; set; }
    public string? Ssousername {get; set; }
    public string? Ssoprovider { get; set; }
    public int? Trusetscore { get; set; }
}