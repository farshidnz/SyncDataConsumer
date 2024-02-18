using System;
using System.Diagnostics.CodeAnalysis;
using Amazon.SQS;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;

namespace AccountSyncData.Integration.Tests
{
    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public sealed class GenericHost<TStartup> : IDisposable where TStartup : class
    {
        private readonly IHost _host;
        private readonly TestServer _server;

        public GenericHost(
            Action<IServiceCollection>? testServices = default, Action<IConfigurationBuilder>? configure = default)
        {
            var hostBuilder = Host
                .CreateDefaultBuilder()
                .ConfigureLogging(builder => builder.AddConsole().AddDebug())
                .ConfigureAppConfiguration(builder => configure?.Invoke(builder))
                .ConfigureHostConfiguration(builder =>
                {
                    builder.AddJsonFile("appsettings.Integration.json", false, false);
                })
                .ConfigureWebHost(
                    builder => builder
                        .ConfigureTestServices(services =>
                        {
                            testServices?.Invoke(services);
                            // by default we want to mock our bus, and we can override it with different
                            // implementations later, in more specific tests.
                            // Or you can obtain the mocked bus and set some expectations.
                            
                            var mockedSqs = Mock.Of<IAmazonSQS>();
                            services.TryAddScoped(_=> mockedSqs);
                            services.TryAddScoped<Mock<IAmazonSQS>>(_=> Mock.Get(mockedSqs));
                        })
                        .UseEnvironment("Integration")
                        .UseTestServer()
                        .UseStartup<TStartup>());

            _host = hostBuilder.Start();

            _server = _host.GetTestServer();

            //
            // We are going to establish a 'default' principal so we can
            // use it when dealing with db contexts in our unit/integration test set ups.
            //
            ServiceProvider = _server.Services;
        }

        public IServiceProvider ServiceProvider { get; }

        public void Dispose()
        {
            _host.Dispose();
            _server.Dispose();
        }
    }
}
