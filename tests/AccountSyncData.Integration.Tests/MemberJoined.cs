using System.Threading.Tasks;
using Xunit;

namespace AccountSyncData.Integration.Tests;

public class MemberJoined : IAsyncLifetime
{
    // private GenericHost<Program> _webHost;

    public Task InitializeAsync()
    {
        throw new System.NotImplementedException();
    }

    public async Task DisposeAsync()
    {
        // _webHost = new GenericHost<Program>(services => { });
        // var config = _webHost.ServiceProvider.GetRequiredService<IConfiguration>();
    }
}