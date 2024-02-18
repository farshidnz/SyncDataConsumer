using System.Data;
using AccountSyncData.Consumer.Models;
using AccountSyncData.Consumer.PIIService;

namespace AccountSyncData.Consumer.Handler;

public class MemberDetailChangedHandler : IMessageHandler
{
    private readonly ILogger _logger;
    private readonly IDbConnection _connection;
    private readonly IEncryptionService _encryptionService;

    public MemberDetailChangedHandler(ILogger<MemberDetailChangedHandler> logger, IDbConnection connection, IEncryptionService encryptionService)
    {
        _logger = logger;
        _connection = connection;
        _encryptionService = encryptionService;
    }

    public async Task<HandlerResponse> HandleAsync(IMessage message)
    {
        _logger.LogInformation("MemberDetailChanged message recieved.");
        
        var memberDetail = (MemberDetailChanged)message;

        var piiData = await _encryptionService.DecryptAsync(memberDetail.PIIData);

        var command = _connection.CreateCommand();
        _connection.Open();
        _connection.BeginTransaction();
        using var dbCommand = _connection.CreateCommand();
        // command.CommandText = $"INSERT database (firstName, lastName ) values ( {memberDetail.FirstName} , {memberDetail.LastName} )";
        command.ExecuteNonQuery();
        _connection.Close();
        return new HandlerResponse(ResponseStatus.Successful, string.Empty);
    }
    public static Type MessageType => typeof(MemberDetailChanged);

}