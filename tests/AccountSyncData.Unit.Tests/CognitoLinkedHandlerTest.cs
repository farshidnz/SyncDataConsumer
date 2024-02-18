using NUnit.Framework;
using System.Data;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;
using Moq;
using Dapper;
using Moq.Dapper;
using AccountSyncData.Consumer.Models;
using AccountSyncData.Consumer.Handler;


namespace AccountSyncData.Unit.Tests;

[TestFixture]
internal class CognitoLinkedHandlerTest : TestBase
{ 
    private CognitoLinked _cognitoLinked;
    private string _decryptPIIData;
    private PIIData _piiData;

    private MemberDto _member1;
    private MemberDto _member2;
    private MemberDto _member3;

    [SetUp]
    public void SetUp()
    {
        _cognitoLinked = new CognitoLinked()
        {
            CognitoPoolId = "testPoolId",
            CognitoId = Guid.Parse("79fd4f45-192f-4ed6-8676-160a7e01a79c"),
            PIIData = "AQICAHiPxktpzxniBrLLT6gI6Xo80ZtGCi3ORj0+sfIbKTRVvQGCObyM44SJ8JGT3hUfZE8CAAAAlTCBkgYJKoZIhvcNAQcGoIGEMIGBAgEAMHwGCSqGSIb3DQEHATAeBglghkgBZQMEAS4wEQQMTvZop9vQ6Yu48A5+AgEQgE8cA23wJOWW6cPQg1ppY9G1VAUFZL5/CUa8T+gjOaiz7LRK2nthO3RA5C2HQF90ygiZ9LuAJt9Dlx5UgHO6andZWWnpE6xamoaYXLOumDFO",
            Metadata = {}
        };
       
        _piiData = new PIIData {
        Email = "qa+signup-felix0008@cashrewards.com"
       };
       
        _decryptPIIData = JsonConvert.SerializeObject(_piiData);

        _member1 =   new MemberDto {
            MemberId = 1001502578,
            MemberNewId = Guid.Parse("202BDAD3-CB87-4B75-AA84-5CB0C5748866"),
            Status = 1,
            PersonId = null
        };

        _member2 =     new MemberDto {
            MemberId = 1001502579,
            MemberNewId = Guid.Parse("614009CF-6811-498F-8511-A4B81B44EA4A"),
            Status = 1,
            PersonId = null
        };

        _member3 =     new MemberDto {
            MemberId = 1001502579,
            MemberNewId = Guid.Parse("614009CF-6811-498F-8511-A4B81B44EA4A"),
            Status = 1,
            PersonId = 1234
        };
    }

    [TestCase(null)]
    [TestCase("")]
    public async Task CognitoLinkedHandler_ShouldDoNothingWhenNoEmailInPIIdata(string piiData)
    {
        var state = new TestState();

        var message = new CognitoLinked()
        {
            CognitoPoolId = "testPoolId",
            CognitoId = Guid.Parse("79fd4f45-192f-4ed6-8676-160a7e01a79c"),
            PIIData = piiData,
            Metadata = {}
        };
       
        await state._CognitoLinkedHandler.HandleAsync((IMessage)message);

        state._encryptionService.Verify(c => c.DecryptAsync(It.IsAny<string>()), Times.Never);
        state._dbConnection.Verify( c => c.Open(), Times.Never);
    }

    [Test]
    public async Task CognitoLinkedHandler_ShouldDoNothingWhenNoMemberFound()
    {
        var state = new TestState();
        var message = _cognitoLinked;

        state._encryptionService.Setup(c => c.DecryptAsync(It.IsAny<string>())).ReturnsAsync(_decryptPIIData);
        state._dbConnection.SetupDapperAsync(c => c.QueryAsync<MemberDto>(It.IsAny<string>(), It.IsAny<object>(), null, null, null));

        await state._CognitoLinkedHandler.HandleAsync((IMessage)message);

        state._encryptionService.Verify(c => c.DecryptAsync(It.IsAny<string>()), Times.Once);
        state._dbConnection.Verify( c => c.Open(), Times.Once);
        state._dbConnection.Verify( c => c.Close(), Times.Once);
        state._dbConnection.Verify( c => c.CreateCommand(), Times.Once);
    }

    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    public async Task CognitoLinkedHandler_ShouldUpdateMemberInDatabaseWhenOneMemberExist(int memberNumber)
    {
        var state = new TestState();
        var message = _cognitoLinked;
        int times = 5;
        List<MemberDto> existingMembers;

        state._encryptionService.Setup(c => c.DecryptAsync(It.IsAny<string>())).ReturnsAsync(_decryptPIIData);
        if(memberNumber == 1)
        {
            existingMembers = new List<MemberDto> { _member1 };
        } else if (memberNumber == 2) {
            existingMembers = new List<MemberDto>() { _member1, _member2 };
            times = 6;
        } else {
            existingMembers = new List<MemberDto>() { _member1, _member3 };
            times = 5;
        }

        state._dbConnection.SetupDapperAsync(c => c.QueryFirstAsync<int>(It.IsAny<string>(), null, null, null, null))
            .ReturnsAsync(2);
        state._dbConnection.SetupDapperAsync(c => c.QueryAsync<MemberDto>(It.IsAny<string>(), It.IsAny<object>(), null, null, null))
            .ReturnsAsync(existingMembers);

        Assert.DoesNotThrowAsync(async () => await state._CognitoLinkedHandler.HandleAsync((IMessage)message));

        state._encryptionService.Verify(c => c.DecryptAsync(It.IsAny<string>()), Times.Once);
        state._dbConnection.Verify(c => c.Open(), Times.Once);
        state._dbConnection.Verify(x => x.CreateCommand(), Times.Exactly(times));

        state._dbConnection.Verify(c => c.BeginTransaction(), Times.Once);
        state._dbConnection.Verify(c => c.Close(), Times.Once);

    }
}
