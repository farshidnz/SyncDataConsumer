using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Moq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AccountSyncData.Consumer;
using AccountSyncData.Consumer.Handler;
using AccountSyncData.Consumer.Models;
using NUnit.Framework.Interfaces;

namespace AccountSyncData.Unit.Tests;

public class MessageDispatcherTests
{
    private Mock<IServiceScopeFactory> mockScopeFactory;
    private Mock<IServiceScope> mockScope;
    private Mock<IServiceProvider> mockServiceProvider;
    private Mock<IMessageHandler> mockHandler;

    public MessageDispatcherTests() {
        mockScopeFactory = new Mock<IServiceScopeFactory>();
        mockScope = new Mock<IServiceScope>();
        mockServiceProvider = new Mock<IServiceProvider>();
        mockHandler = new Mock<IMessageHandler>();
    }

    public class TMessage : IMessage
    {
        public string MessageTypeName => nameof(TMessage);
    }

    [SetUp]
    public void Setup()
    {
        mockScopeFactory.Setup(x => x.CreateScope()).Returns(mockScope.Object);
        mockScope.Setup(x => x.ServiceProvider).Returns(mockServiceProvider.Object);
        mockServiceProvider.Setup(x => x.GetService(typeof(CognitoLinkedHandler))).Returns(mockHandler.Object);
    }

    [Test]
    public async Task DispatchAsync_ValidInput_CallsHandleAsync()
    {
        var message = new CognitoLinked();
        var dispatcher = new MessageDispatcher(mockScopeFactory.Object);
        await dispatcher.DispatchAsync(message);
        // Assert
        mockHandler.Verify(x => x.HandleAsync(message), Times.Once);
    }

    [Test]
    public void CanHandleMessageType_ValidInput_ReturnsTrue()
    {
        var messageTypeName = nameof(CognitoLinked);
        var dispatcher = new MessageDispatcher(mockScopeFactory.Object);
        var result = dispatcher.CanHandleMessageType(messageTypeName);
       
        Assert.True(result);
    }

    [Test]
    public void CanHandleMessageType_InvalidInput_ReturnsFalse()
    {
        var messageTypeName = "InvalidType";
        var dispatcher = new MessageDispatcher(mockScopeFactory.Object);
        var result = dispatcher.CanHandleMessageType(messageTypeName);

        Assert.False(result);
    }

    [Test]
    public void CanHandleMessageType_InvalidInput_ReturnsNull()
    {
        var messageTypeName = "InvalidType";
        var dispatcher = new MessageDispatcher(mockScopeFactory.Object);
        var result = dispatcher.GetMessageTypeByName(messageTypeName);

        Assert.Null(result);
    }

    [Test]
    public void GetMessageTypeByName_ValidInput_ReturnsType()
    {
        var messageTypeName = nameof(CognitoLinked);
        var dispatcher = new MessageDispatcher(mockScopeFactory.Object);
        var result = dispatcher.GetMessageTypeByName(messageTypeName);

        Assert.AreEqual(typeof(CognitoLinked), result);
    }
}
