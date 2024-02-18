using System.Data;
using Moq;
using Dapper;
using Moq.Dapper;
using AccountSyncData.Consumer.Models;
using System.Data.Common;

namespace AccountSyncData.Unit.Tests;

public class MockIDbConnection : Mock<IDbConnection>
{
    public MockIDbConnection() 
    {
        Setup(m => m.CreateCommand()).Returns(new MockIDbCommand().Object);
        Setup(m => m.Open());
        Setup(m => m.Close());
    }
}

public class MockIDbCommand : Mock<IDbCommand>
{
    public MockIDbCommand()
    {
        Setup(m => m.ExecuteNonQuery()).Returns(It.IsAny<int>());
        Setup(m => m.CreateParameter()).Returns(It.IsAny<IDbDataParameter>());
    }
}


public class MockDbTransaction : Mock<DbTransaction>
{
    public MockDbTransaction()
    {
        Setup(m => m.Commit());
    }
}
