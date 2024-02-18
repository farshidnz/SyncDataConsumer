using System.Data;
using System.Security;
using AccountSyncData.Consumer.Encryption;
using AccountSyncData.Consumer.Models;
using AccountSyncData.Consumer.PIIService;
using Dapper;

namespace AccountSyncData.Consumer.Handler;

public class CognitoLinkedHandler : IMessageHandler
{
    private readonly ILogger _logger;
    private readonly IDbConnection _connection;
    private readonly IEncryptionService _encryptionService;

    public CognitoLinkedHandler(ILogger<CognitoLinkedHandler> logger, IDbConnection connection,
        IEncryptionService encryptionService)
    {
        _logger = logger;
        _connection = connection;
        _encryptionService = encryptionService;
    }

    public async Task<HandlerResponse> HandleAsync(IMessage message)
    {
        _logger.LogInformation("CognitoLinked message recieved.");
        var cognitoLinked = (CognitoLinked)message;

        PIIData? piiData = null;

        if (!string.IsNullOrWhiteSpace(cognitoLinked.PIIData))
        {
            piiData = await _encryptionService.Decrypt(cognitoLinked.PIIData);
        }

        if (piiData == null || string.IsNullOrWhiteSpace(piiData.Email))
        {
            return new HandlerResponse(ResponseStatus.Failed, "No PiIData with Email was decrypted.");
        }

        try
        {
            _connection.Open();
            List<MemberDto> existingMembers = await GetMembersByEmail(piiData.Email);
            if(existingMembers == null || existingMembers.Count == 0)
            {
                _connection.Close();            
                return new HandlerResponse(ResponseStatus.Failed, $"No Member was found for email: {piiData.Email}");
            }

            var isPersonExisted = existingMembers.Any<MemberDto>( x => x.PersonId != null);
            if(!isPersonExisted) {
                InsertPerson(cognitoLinked);
            }

            int personId = await GetPersonId(cognitoLinked);

            using var transaction = _connection.BeginTransaction();
            existingMembers.ForEach(member => InsertOrUpdateCognitoMember(transaction, member, cognitoLinked, personId));
            UpdateMemberPersonId(transaction, personId, piiData.Email);

            transaction.Commit();

            _connection.Close();
            return new HandlerResponse(ResponseStatus.Successful, string.Empty);

        }
        catch (Exception e)
        {
            _connection.Close();
            _logger.LogError(e, $"Failed to update cognitioId - {cognitoLinked} in database for Account:{piiData.Email}");
            return new HandlerResponse(ResponseStatus.Failed, $"Failed to update cognitioId - {cognitoLinked} in database for Account:{piiData.Email}");
        }
    }

    public async Task<List<MemberDto>> GetMembersByEmail(string email)
    {
        var query = @"SELECT MemberId, MemberNewId, Status, PersonId
                                FROM dbo.Member
                                WHERE Email = @Email";

        var member = await _connection.QueryAsync<MemberDto>(query, new
        {
            Email = email
        });

        return member.ToList();
    }

    private void InsertPerson(CognitoLinked cognitoLinked)
    {
        using var dbCommand = _connection.CreateCommand();

        dbCommand.CommandText = $"INSERT INTO Person" +
                                $"(CognitoId" +
                                $")" +
                                $"values ( @CognitoId " +
                                $"); SELECT CAST(SCOPE_IDENTITY() as int)";

        var cognitoId = dbCommand.CreateParameter();
        cognitoId.ParameterName = @"@CognitoId";
        cognitoId.DbType = DbType.Guid;
        cognitoId.Value = cognitoLinked.CognitoId;
        dbCommand.Parameters.Add(cognitoId);

        dbCommand.ExecuteNonQuery();
    }

    private async Task<int> GetPersonId(CognitoLinked cognitoLinked)
    {
        int personId =  await _connection.QueryFirstAsync<int>(
                $"SELECT TOP(1) PersonId FROM Person WHERE CognitoId = @CognitoId", new { CognitoId = cognitoLinked.CognitoId });

        return personId;
    }


    private void UpdateMemberPersonId(IDbTransaction dbTransaction, int personId, string email)
    {
        using var dbCommandCognito = _connection.CreateCommand();
        dbCommandCognito.Transaction = dbTransaction;
        dbCommandCognito.CommandText = $"UPDATE Member SET " +
                                       $"PersonId = @PersonId " +
                                       $"Where Email = @Email;";

        var PersonId = dbCommandCognito.CreateParameter();
        PersonId.ParameterName = @"@PersonId";
        PersonId.DbType = DbType.Int32;
        PersonId.Value = personId;
        dbCommandCognito.Parameters.Add(PersonId);

        var Email = dbCommandCognito.CreateParameter();
        Email.ParameterName = @"@Email";
        Email.DbType = DbType.String;
        Email.Value = email;
        dbCommandCognito.Parameters.Add(Email);

        dbCommandCognito.ExecuteNonQuery();
    }

    private void InsertOrUpdateCognitoMember(IDbTransaction dbTransaction, MemberDto member, CognitoLinked cognitoLinked, int personId)
    {
        using var dbCommandCognito = _connection.CreateCommand();
        dbCommandCognito.Transaction = dbTransaction;
        var insertSqlString = $"INSERT INTO CognitoMember" +
                                       $"(CognitoId," +
                                       $"MemberId," +
                                       $"CognitopoolId," +
                                       $"MemberNewId, " +
                                       $"Status," +
                                       $"PersonId" +
                                       $")" +
                                       $"values ( @CognitoId, " +
                                       $"@MemberId," +
                                       $"@CognitoPoolId," +
                                       $"@MemberNewId, " +
                                       $"@Status," +
                                       $"@PersonId" +
                                       $"); SELECT CAST(SCOPE_IDENTITY() as int)";
        var updateSqlString = $"UPDATE CognitoMember SET CognitoId = @CognitoId, " +
                                        $"CognitoPoolId = @CognitoPoolId " +
                                        $" WHERE PersonId = @PersonId";

        dbCommandCognito.CommandText = member.PersonId == null ? insertSqlString : updateSqlString;
        var CognitoId = dbCommandCognito.CreateParameter();
        CognitoId.ParameterName = @"@CognitoId";
        CognitoId.DbType = DbType.Guid;
        CognitoId.Value = cognitoLinked.CognitoId;
        dbCommandCognito.Parameters.Add(CognitoId);

        var CognitoPoolId = dbCommandCognito.CreateParameter();
        CognitoPoolId.ParameterName = @"@CognitoPoolId";
        CognitoPoolId.DbType = DbType.String;
        CognitoPoolId.Value = cognitoLinked.CognitoPoolId;
        dbCommandCognito.Parameters.Add(CognitoPoolId);

        var PersonId = dbCommandCognito.CreateParameter();
        PersonId.ParameterName = @"@PersonId";
        PersonId.DbType = DbType.Int32;
        PersonId.Value = personId;
        dbCommandCognito.Parameters.Add(PersonId);

        if(member.PersonId == null) {
            var MemberId = dbCommandCognito.CreateParameter();
            MemberId.ParameterName = @"@MemberId";
            MemberId.DbType = DbType.Int32;
            MemberId.Value = member.MemberId;
            dbCommandCognito.Parameters.Add(MemberId);

            var MemberNewId = dbCommandCognito.CreateParameter();
            MemberNewId.ParameterName = @"@MemberNewId";
            MemberNewId.DbType = DbType.Guid;
            MemberNewId.Value = member.MemberNewId;
            dbCommandCognito.Parameters.Add(MemberNewId);

            var Status = dbCommandCognito.CreateParameter();
            Status.ParameterName = @"@Status";
            Status.DbType = DbType.Int32;
            Status.Value = member.Status;
            dbCommandCognito.Parameters.Add(Status);
        }

        dbCommandCognito.ExecuteNonQuery();
    }

    public static Type MessageType => typeof(CognitoLinked);
}
