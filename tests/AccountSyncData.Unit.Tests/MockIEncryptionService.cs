using AccountSyncData.Consumer.PIIService;
using Moq;

namespace AccountSyncData.Unit.Tests;

public class MockIEncryptionService : Mock<IEncryptionService>
{
    public MockIEncryptionService()
    {
        Setup(r => r.DecryptAsync(It.IsAny<string>()));
    }
}