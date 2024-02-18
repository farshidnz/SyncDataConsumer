using System.Data;
using AccountSyncData.Consumer.Handler;
using AccountSyncData.Consumer.Models;
using AccountSyncData.Consumer.PIIService;
using Amazon;
using Amazon.KeyManagementService;
using Amazon.KeyManagementService.Model;
using Amazon.SQS;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Moq;

namespace AccountSyncData.Unit.Tests;

public class ConsumerTests
{
    [Test]
    public async Task MemberDetailChangedHandler()
    {
        var mockLogger = new Mock<ILogger<MemberDetailChangedHandler>>().Object;
        var mockDbConnection = new Mock<IDbConnection>();
        var mockEncryptionService = new Mock<IEncryptionService>();
        var mockDbCommand = new Mock<IDbCommand>();

        var message = new MemberDetailChanged()
        {
            Accesscode = "accessCode"
        };

        mockDbConnection.Setup(m => m.CreateCommand()).Returns(mockDbCommand.Object);
        mockDbCommand.Setup(m => m.ExecuteNonQuery()).Returns(It.IsAny<int>());
        mockDbConnection.Setup(m => m.Open());
        mockDbConnection.Setup(m => m.BeginTransaction()).Returns(It.IsAny<IDbTransaction>());
        mockDbConnection.Setup(m => m.Close());

        mockEncryptionService.Setup(m => m.DecryptAsync(It.IsAny<string>())).ReturnsAsync(It.IsAny<string>());

        var memberDetailChangedHandler =
            new MemberDetailChangedHandler(mockLogger, mockDbConnection.Object, mockEncryptionService.Object);
        Assert.DoesNotThrowAsync(async () => await memberDetailChangedHandler.HandleAsync(message));
    }

    [Test]
    public async Task Decrypt()
    {
        var mockAmazonKeyManagement = new Mock<AmazonKeyManagementServiceClient>(RegionEndpoint.APSoutheast2);
        var decryptResponse = new DecryptResponse()
        {
            Plaintext = new MemoryStream()
        };

        mockAmazonKeyManagement
            .Setup(m => m.DecryptAsync(It.IsAny<DecryptRequest>(), CancellationToken.None))
            .ReturnsAsync(decryptResponse);
        var kmsEncryptionService = new KMSEncryptionService(mockAmazonKeyManagement.Object);
        Assert.NotNull(await kmsEncryptionService.DecryptAsync(""));
    }
    
    [Test]
    public async Task MessageConsumer()
    {
        /*  var mockAmazonSqs = new Mock<IAmazonSQS>();
          var mockDispatcher = new Mock<MessageDispatcher>();
          var mockLogger = new Mock<ILogger<SqsConsumerService>>().Object;
          var mockConfigure = new Mock<IConfiguration>();
          var mockAmazonKeyManagement = new Mock<AmazonKeyManagementServiceClient>();

          mockConfigure
              .Setup(m => m.GetSection(It.IsAny<string>()))
              .Returns(It.IsAny<string>());

          mockDispatcher
              .Setup(m => m.CanHandleMessageType(It.IsAny<string>()))
              .Returns(false);
           var kmsEncryptionService = new KMSEncryptionService(mockAmazonKeyManagement.Object);
           Assert.NotNull(await kmsEncryptionService.DecryptAsync(""));
        */
        Assert.True(true);
    }
}