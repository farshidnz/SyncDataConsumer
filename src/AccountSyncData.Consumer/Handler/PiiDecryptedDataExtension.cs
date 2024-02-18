using System.Text.Json;
using AccountSyncData.Consumer.Models;
using AccountSyncData.Consumer.PIIService;

namespace AccountSyncData.Consumer.Handler;

public static class PiiDecryptedDataExtension
{
    public static async Task<PIIData?> Decrypt(this IEncryptionService encryptionService, string piiData)
    {
        var decryptedPiiData = await encryptionService.DecryptAsync(piiData);
        return JsonSerializer.Deserialize<PIIData>(decryptedPiiData);
    }
}