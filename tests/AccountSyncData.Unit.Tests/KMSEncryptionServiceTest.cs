using AccountSyncData.Consumer.PIIService;
using Amazon;
using Amazon.KeyManagementService.Model;
using Amazon.KeyManagementService;
using Moq;
using System.Text;
using NUnit.Framework;

namespace AccountSyncData.Unit.Tests;

internal class KMSEncryptionServiceTest
{
   // Init Common Test Data 
        private const string KeyArn = "arn:aws:kms:ap-southeast-2:752830773963:key/2ef257a0-1111-1111-1111-230f7f3b5906";
        private const string TestString = "mysecret";

        // Mocks
        Mock<AmazonKeyManagementServiceClient> kmsClient;

        [SetUp]
        public void Setup()
        {
            kmsClient = new Mock<AmazonKeyManagementServiceClient>(RegionEndpoint.APSoutheast2);
           
            DecryptResponse decryptResponse = new();
            kmsClient.Setup(x => x.DecryptAsync(It.IsAny<DecryptRequest>(), default)).ReturnsAsync(decryptResponse);
        }

        public KMSEncryptionService SUT()
        {
            return new KMSEncryptionService(kmsClient.Object);
        }

        [TestCase(null)]
        [TestCase("")]
        public async Task WhenInputIsNullOrEmpty_ShouldReturnEmptyString(string input)
        {
            string encryptString = await SUT().DecryptAsync(input);
            Assert.IsEmpty(encryptString);
        }

        [Test]
        public async Task WhenDecryptAsync_ShouldReturnDecryptedString()
        {
            var testString = TestString;

            DecryptResponse decryptResponse = new();
            var decryptData = Encoding.UTF8.GetBytes(TestString);
            decryptResponse.Plaintext = new System.IO.MemoryStream(decryptData, 0, decryptData.Length);
            kmsClient.Setup(x => x.DecryptAsync(It.IsAny<DecryptRequest>(), default)).ReturnsAsync(decryptResponse);

            var encryptStringBase64 = await SUT().DecryptAsync(testString);
            Assert.IsNotEmpty(encryptStringBase64);
        }
}
