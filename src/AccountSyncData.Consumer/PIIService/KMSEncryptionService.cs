using System.Text;
using Amazon.KeyManagementService;
using Amazon.KeyManagementService.Model;

namespace AccountSyncData.Consumer.PIIService;

public class KMSEncryptionService : IEncryptionService
{
    private readonly AmazonKeyManagementServiceClient _client;

    public KMSEncryptionService(AmazonKeyManagementServiceClient client)
    {
        _client = client;
    }

    // It's used a sysymmetric KMS key to encrypt/decrypt the data which size is less then 4K, the key is included in the metadata,
    // for the decrypt function, no need to provide the keyId.

    /// <summary>
    /// Decrypts string information into base64 string or string
    /// </summary>
    /// <param name="encryptedData">data to be decrypted</param>
    /// <param name="dataType">String or Base64</param>
    /// <param name="encryptAlgorithm"></param>
    /// <returns>String</returns>
    public async Task<string> DecryptAsync(string encryptedData)
    {
        if (string.IsNullOrEmpty(encryptedData)) return string.Empty;

        var textBytes = Convert.FromBase64String(encryptedData);

        var decryptRequest = new DecryptRequest
        {
            CiphertextBlob = new MemoryStream(textBytes, 0, textBytes.Length),
        };


        var response = await _client.DecryptAsync(decryptRequest);
        return response != null ? Encoding.UTF8.GetString(response.Plaintext.ToArray()) : string.Empty;
    }
}