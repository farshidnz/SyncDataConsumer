namespace AccountSyncData.Consumer.Models;

public class MemberDto
{
    public int MemberId { get; set; }
    public int? ClientId { get; set; }
    public int? OriginationSource { get; set; } 
    public DateTime? DateJoined { get; set; }  // It's UTC format
    public Guid? MemberNewId { get; set; }
    public Boolean? IsValidated { get; set; }
    public int? Status { get; set; }
    public int? PersonId { get; set; }
}
