using System.Data;
using System.Security;
using AccountSyncData.Consumer.Encryption;
using AccountSyncData.Consumer.Models;
using AccountSyncData.Consumer.PIIService;
using Dapper;

namespace AccountSyncData.Consumer.Handler;

public class MemberJoinedHandler : IMessageHandler
{
    private readonly ILogger _logger;
    private readonly IDbConnection _connection;

    private readonly IEncryptionService _encryptionService;


    public MemberJoinedHandler(ILogger<MemberJoinedHandler> logger, IDbConnection connection,
        IEncryptionService encryptionService)
    {
        _logger = logger;
        _connection = connection;
        _encryptionService = encryptionService;
    }

    public async Task<HandlerResponse> HandleAsync(IMessage message)
    {
        _logger.LogInformation("MemberJoined message recieved.");
        var memberJoined = (MemberJoined) message;

        PIIData? piiData = null;
        if (!string.IsNullOrWhiteSpace(memberJoined.PIIData))
        {
            piiData = await _encryptionService.Decrypt(memberJoined.PIIData);
        }

        if (piiData == null || string.IsNullOrWhiteSpace(piiData.Email))
        {
            return new HandlerResponse(ResponseStatus.Failed, "No PiIData was decrypted." );
        }

        var salt = SHACryptor.GenerateSaltKey(20);
        try
        {
            _connection.Open();
            if (await MemberExist(piiData.Email) || await CognitoIdExist(memberJoined.CognitoId))
            {
                _connection.Close();
                return new HandlerResponse(ResponseStatus.Failed, "Member already exist.");
            }

            using var transaction = _connection.BeginTransaction();

            SetIdentityInsert(transaction);

            InsertMember(transaction, memberJoined, piiData, salt);
            InsertPerson(transaction, memberJoined);

            transaction.Commit();

            await InsertCognitoMember(memberJoined);

            _connection.Close();
            return new HandlerResponse(ResponseStatus.Successful, string.Empty);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "");
            _connection.Close();   
            return new HandlerResponse(ResponseStatus.Failed, "Failed to insert new joined member.");
        }
    }

    private async Task<bool> MemberExist(string email)
    {
        return await _connection.QueryFirstAsync<bool>(
            $"SELECT CASE WHEN EXISTS ( SELECT * FROM Member WHERE Email = @Email ) THEN CAST(1 AS BIT)ELSE CAST(0 AS BIT) END", new
        {
            Email = email
        });
    }
    
    private async Task<bool> CognitoIdExist(Guid cognitoId)
    {
        return await _connection.QueryFirstAsync<bool>(
            $"SELECT CASE WHEN EXISTS ( SELECT * FROM Person WHERE CognitoId = @CognitoId ) THEN CAST(1 AS BIT)ELSE CAST(0 AS BIT) END", new
        {
            CognitoId = cognitoId
        });
    }


    private async Task InsertCognitoMember(MemberJoined memberJoined)
    {
        var personId =
            await _connection.QueryFirstAsync<int>(
                $"SELECT TOP(1) PersonId FROM Person WHERE CognitoId = @CognitoId", new
        {
            CognitoId = memberJoined.CognitoId
        });

        using var dbCommandCognito = _connection.CreateCommand();

        dbCommandCognito.CommandText = $"INSERT INTO CognitoMember" +
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

        var CognitoId = dbCommandCognito.CreateParameter();
        CognitoId.ParameterName = @"@CognitoId";
        CognitoId.DbType = DbType.Guid;
        CognitoId.Value = memberJoined.CognitoId;
        dbCommandCognito.Parameters.Add(CognitoId);

        var MemberId = dbCommandCognito.CreateParameter();
        MemberId.ParameterName = @"@MemberId";
        MemberId.DbType = DbType.Int32;
        MemberId.Value = memberJoined.MemberId;
        dbCommandCognito.Parameters.Add(MemberId);

        var CognitoPoolId = dbCommandCognito.CreateParameter();
        CognitoPoolId.ParameterName = @"@CognitoPoolId";
        CognitoPoolId.DbType = DbType.String;
        CognitoPoolId.Value = memberJoined.CognitoPoolId;
        dbCommandCognito.Parameters.Add(CognitoPoolId);

        var MemberNewId = dbCommandCognito.CreateParameter();
        MemberNewId.ParameterName = @"@MemberNewId";
        MemberNewId.DbType = DbType.Guid;
        MemberNewId.Value = memberJoined.MemberNewId;
        dbCommandCognito.Parameters.Add(MemberNewId);

        var Status = dbCommandCognito.CreateParameter();
        Status.ParameterName = @"@Status";
        Status.DbType = DbType.Int32;
        Status.Value = memberJoined.Status;
        dbCommandCognito.Parameters.Add(Status);

        var PersonId = dbCommandCognito.CreateParameter();
        PersonId.ParameterName = @"@PersonId";
        PersonId.DbType = DbType.Int32;
        PersonId.Value = personId;
        dbCommandCognito.Parameters.Add(PersonId);

        dbCommandCognito.ExecuteNonQuery();
    }

    private void InsertPerson(IDbTransaction dbTransaction, MemberJoined memberJoined)
    {
        using var dbCommand = _connection.CreateCommand();
        dbCommand.Transaction = dbTransaction;

        dbCommand.CommandText = $"INSERT INTO Person" +
                                $"(CognitoId" +
                                $")" +
                                $"values ( @CognitoId " +
                                $") SELECT CAST(SCOPE_IDENTITY() as int)";

        var CognitoId = dbCommand.CreateParameter();
        CognitoId.ParameterName = @"@CognitoId";
        CognitoId.DbType = DbType.Guid;
        CognitoId.Value = memberJoined.CognitoId;
        dbCommand.Parameters.Add(CognitoId);

        dbCommand.ExecuteNonQuery();
    }

    private void InsertMember(IDbTransaction dbTransaction, MemberJoined memberJoined, PIIData piiData, string salt)
    {
        using var dbCommand = _connection.CreateCommand();
        dbCommand.Transaction = dbTransaction;

        dbCommand.CommandText = $"INSERT INTO Member" +
                                $"(MemberId," +
                                $"ClientId," +
                                $"Status," +
                                $"Email, " +
                                $"FirstName, " +
                                $"LastName, " +
                                $"SaltKey, " +
                                $"PostCode, " +
                                $"DateOfBirth, " +
                                $"HashedEmail, " +
                                $"Mobile, " +
                                $"UserPassword" +
                                $")" +
                                $"values ( @MemberId, " +
                                $"@ClientId," +
                                $"@Status," +
                                $"@Email, " +
                                $"@FirstName," +
                                $"@LastName," +
                                $"@Salt," +
                                $"@postCode," +
                                $"@dateOfBirth," +
                                $"@hashedEmail," +
                                $"@mobile," +
                                $"@userPassword" +
                                $")";

        var MemberId = dbCommand.CreateParameter();
        MemberId.ParameterName = @"@MemberId";
        MemberId.DbType = DbType.Int32;
        MemberId.Value = memberJoined.MemberId;
        dbCommand.Parameters.Add(MemberId);

        var ClientId = dbCommand.CreateParameter();
        ClientId.ParameterName = @"@ClientId";
        ClientId.DbType = DbType.Int32;
        ClientId.Value = memberJoined.ClientId;
        dbCommand.Parameters.Add(ClientId);

        var Status = dbCommand.CreateParameter();
        Status.ParameterName = @"@Status";
        Status.DbType = DbType.Int32;
        Status.Value = memberJoined.Status;
        dbCommand.Parameters.Add(Status);

        var Email = dbCommand.CreateParameter();
        Email.ParameterName = @"@Email";
        Email.DbType = DbType.String;
        Email.Value = (string.IsNullOrWhiteSpace(piiData.Email) ? string.Empty : piiData.Email);
        dbCommand.Parameters.Add(Email);

        var FirstName = dbCommand.CreateParameter();
        FirstName.ParameterName = @"@FirstName";
        FirstName.DbType = DbType.String;
        FirstName.Value = (string.IsNullOrWhiteSpace(piiData.FirstName) ? string.Empty : piiData.FirstName);
        dbCommand.Parameters.Add(FirstName);

        var LastName = dbCommand.CreateParameter();
        LastName.ParameterName = @"@LastName";
        LastName.DbType = DbType.String;
        LastName.Value = (string.IsNullOrWhiteSpace(piiData.LastName) ? string.Empty : piiData.LastName);
        dbCommand.Parameters.Add(LastName);

        var Salt = dbCommand.CreateParameter();
        Salt.ParameterName = @"@Salt";
        Salt.DbType = DbType.String;
        Salt.Value = salt;
        dbCommand.Parameters.Add(Salt);

        var postcode = dbCommand.CreateParameter();
        postcode.ParameterName = @"@postCode";
        postcode.DbType = DbType.String;
        postcode.Value = (string.IsNullOrWhiteSpace(piiData.Postcode) ? string.Empty : piiData.Postcode);
        dbCommand.Parameters.Add(postcode);

        var dateOfBirth = dbCommand.CreateParameter();
        dateOfBirth.ParameterName = @"@dateOfBirth";
        dateOfBirth.DbType = DbType.DateTime2;
        dateOfBirth.Value = piiData.DateOfBirth;
        dbCommand.Parameters.Add(dateOfBirth);

        var hashedEmail = dbCommand.CreateParameter();
        hashedEmail.ParameterName = @"@hashedEmail";
        hashedEmail.DbType = DbType.String;
        hashedEmail.Value = (string.IsNullOrWhiteSpace(piiData.Email) ? string.Empty : piiData.Email);
        dbCommand.Parameters.Add(hashedEmail);

        var mobile = dbCommand.CreateParameter();
        mobile.ParameterName = @"@mobile";
        mobile.DbType = DbType.String;
        mobile.Value = (string.IsNullOrWhiteSpace(piiData.Phone) ? string.Empty : piiData.Phone);
        dbCommand.Parameters.Add(mobile);

        var userPassword = dbCommand.CreateParameter();
        userPassword.ParameterName = @"@userPassword";
        userPassword.DbType = DbType.String;
        userPassword.Value = Guid.NewGuid().ToString();
        dbCommand.Parameters.Add(userPassword);
        dbCommand.ExecuteNonQuery();
    }

    private void SetIdentityInsert(IDbTransaction dbTransaction)
    {
        using var identitySet = _connection.CreateCommand();
        identitySet.Transaction = dbTransaction;
        identitySet.CommandText = "SET IDENTITY_INSERT [Member] ON";
        identitySet.ExecuteNonQuery();
    }
    public static Type MessageType => typeof(MemberJoined);
}