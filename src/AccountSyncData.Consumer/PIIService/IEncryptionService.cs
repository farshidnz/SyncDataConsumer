namespace AccountSyncData.Consumer.PIIService;

public interface IEncryptionService
{
    Task<string> DecryptAsync(string encryptedData);
}