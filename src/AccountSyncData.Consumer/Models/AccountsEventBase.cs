namespace AccountSyncData.Consumer.Models
{
    public class AccountsEventBase
    {
        public EventMetadata? Metadata { get; set; }
        public Guid CognitoId { get; set; }
        public string PIIData { get; set; } = default!;
        public string? PIIEncryptKeyAlias { get; set; }
        public string? PIIIEncryptAlgorithm { get; set; }
    }
}
