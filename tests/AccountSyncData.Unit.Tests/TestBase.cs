using Microsoft.Extensions.Logging;
using Moq;
using System;
using Dapper;
using Moq.Dapper;
using System.Linq.Expressions;
using AccountSyncData.Consumer.Handler;
using AccountSyncData.Consumer.PIIService;
using System.Data;
using Castle.DynamicProxy;

namespace AccountSyncData.Unit.Tests;

internal class TestState
{
    public MockIDbConnection _dbConnection { get; }
    public MockDbTransaction _dbTransaction { get; }
    
    public MockIEncryptionService _encryptionService { get; }
    public MemberDetailChangedHandler _MemberDetailChangedHandler { get; }
    public MemberJoinedHandler _MemberJoinedHandler { get; }
    public CognitoLinkedHandler _CognitoLinkedHandler { get; set; }

    public TestState()
    {
        _dbConnection = new MockIDbConnection();
        _dbConnection.Invocations.Clear();
        _encryptionService = new MockIEncryptionService();

        _dbTransaction = new MockDbTransaction();
        _dbConnection
            .Setup(x => x.BeginTransaction())
            .Returns(() => _dbTransaction.Object);
     
        _MemberDetailChangedHandler = new MemberDetailChangedHandler(new Mock<ILogger<MemberDetailChangedHandler>>().Object, _dbConnection.Object, _encryptionService.Object);
        _MemberJoinedHandler = new MemberJoinedHandler(new Mock<ILogger<MemberJoinedHandler>>().Object, _dbConnection.Object, _encryptionService.Object);
        _CognitoLinkedHandler = new CognitoLinkedHandler(new Mock<ILogger<CognitoLinkedHandler>>().Object, _dbConnection.Object, _encryptionService.Object);
    }
}

public class TestBase
{
    public Expression<Action<ILogger<T>>> CheckLogMesssageMatches<T>(LogLevel logLevel, string logMsg)
    {
        return x => x.Log(logLevel,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((o, t) => string.Equals(logMsg, o.ToString(), StringComparison.InvariantCultureIgnoreCase)),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>());
    }
}

