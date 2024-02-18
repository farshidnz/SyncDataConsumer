using System.Data;
using AccountSyncData.Consumer;
using AccountSyncData.Consumer.Encryption;
using AccountSyncData.Consumer.Handler;
using AccountSyncData.Consumer.PIIService;
using Amazon;
using Amazon.KeyManagementService;
using Amazon.SQS;
using Microsoft.Data.SqlClient;
using Microsoft.Data.SqlClient.AlwaysEncrypted.AzureKeyVaultProvider;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using SqlConnection = Microsoft.Data.SqlClient.SqlConnection;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;
var _clientCredential = new ClientCredential(config["AzureAADClientId"], config["AzureAADClientSecret"]);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

var connectionString = $"Data Source={config["SQLServerHostWriter"]};" +
                       $"Initial Catalog={config["ShopGoDBName"]};" +
                       $"user id={config["ShopGoDBUser"]};" +
                       $"password={config["ShopGoDBPassword"]};" +
                       $"Max Pool Size=1000;" +
                       $"Column Encryption Setting=enabled;" +
                       $"ENCRYPT=yes;" +
                       $"trustServerCertificate=true";

builder.Services.AddHostedService<SqsConsumerService>();

builder.Services.AddSingleton<IAmazonSQS>(_ => new AmazonSQSClient(RegionEndpoint.APSoutheast2));

var azureKeyVaultProvider = new SqlColumnEncryptionAzureKeyVaultProvider(async (authority, resource, scope) =>
{
    var authContext = new AuthenticationContext(authority);
    var result = await authContext.AcquireTokenAsync(resource, _clientCredential);

    if (result == null)
        throw new InvalidOperationException("Failed to obtain the access token");
    return result.AccessToken;
});

Dictionary<string, SqlColumnEncryptionKeyStoreProvider> providers =
    new Dictionary<string, SqlColumnEncryptionKeyStoreProvider>
    {
        {SqlColumnEncryptionAzureKeyVaultProvider.ProviderName, azureKeyVaultProvider}
    };

SqlConnection.RegisterColumnEncryptionKeyStoreProviders(providers);

builder.Services.AddScoped<IDbConnection>(_ =>
{
    var connection = new SqlConnection(connectionString);
    return connection;
});

builder.Services.AddTransient<IEncryptionService, KMSEncryptionService>(_ =>
    new KMSEncryptionService(new AmazonKeyManagementServiceClient(RegionEndpoint.APSoutheast2)));

builder.Services.AddSingleton<MessageDispatcher>();

if(!(config["Environment"] == "live" || config["Environment"] == "prelive"))
{ // Not deploy two event handlers to production since they're not ready
    builder.Services.AddScoped<MemberJoinedHandler>();
    builder.Services.AddScoped<MemberDetailChangedHandler>();
}
builder.Services.AddScoped<CognitoLinkedHandler>();
builder.Services.AddMessageHandlers();

builder.Services.AddHealthChecks()
    .AddSqlServer(connectionString, tags: new[] {"ready"})
    .AddSqs(options => options.RegionEndpoint = RegionEndpoint.APSoutheast2);

var app = builder.Build();

app.MapHealthChecks("/health-check");
await app.RunAsync();

public static partial class Program
{
}