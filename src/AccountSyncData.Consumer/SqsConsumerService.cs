using System.Net;
using System.Reflection;
using System.Text.Json;
using AccountSyncData.Consumer.Handler;
using AccountSyncData.Consumer.Models;
using Amazon.SQS;
using Amazon.SQS.Model;

namespace AccountSyncData.Consumer;

public class SqsConsumerService : BackgroundService
{
    private readonly IAmazonSQS _sqs;
    private readonly IConfiguration _configuration;
    private readonly MessageDispatcher _dispatcher;
    private readonly List<string> _messageAttributeNames = new() { "EventType", "EventID", "EventSource", "Domain", "CorrelationID", "RaisedDateTimeUTC", "PublishedDateTimeUTC" };
    private readonly List<string> _attributeNames = new() { "All" };
    private readonly ILogger _logger;

    public SqsConsumerService(IAmazonSQS sqs, MessageDispatcher dispatcher, ILogger<SqsConsumerService> logger, IConfiguration configuration)
    {
        _sqs = sqs;
        _dispatcher = dispatcher;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var queueName = $"{_configuration["Environment"]}-syncdataconsumer-Member";
        _logger.LogInformation($"Starting the sqs service - listenning to queue {queueName}.");
        var queueUrl = await _sqs.GetQueueUrlAsync(queueName, stoppingToken);

        var receiveRequest = new ReceiveMessageRequest
        {
            QueueUrl = queueUrl.QueueUrl,
            MessageAttributeNames = _messageAttributeNames,
            AttributeNames = _attributeNames,
            MaxNumberOfMessages = 10,
            WaitTimeSeconds = 0
        };

        while (!stoppingToken.IsCancellationRequested)
        {
            var messageResponse = await _sqs.ReceiveMessageAsync(receiveRequest, stoppingToken);
            
            var messages = messageResponse.Messages;
            receiveRequest.WaitTimeSeconds  = 0;
            if (messageResponse.HttpStatusCode != HttpStatusCode.OK)
            {
                _logger.LogWarning($"Message queue response : {messageResponse.HttpStatusCode}, {messages}" );
                continue;
            }

            if(!messages.Any())
            {
                receiveRequest.WaitTimeSeconds = 1;
                continue;
            }

            await ProcessMessages(messages, queueUrl, stoppingToken);
        }
    }

    private async Task ProcessMessages(List<Message> messages, GetQueueUrlResponse queueUrl, CancellationToken stoppingToken)
    {
        foreach (var message in messages)
        {
            _logger.LogInformation($"Found a message. {message.MessageId}");

            var messageTypeName = message.MessageAttributes
                .GetValueOrDefault("EventType")?.StringValue;

            _logger.LogInformation($"Found a message type: . {messageTypeName}");

            if (messageTypeName is null || !_dispatcher.CanHandleMessageType(messageTypeName))
            {
                continue;
            }

            var messageType = _dispatcher.GetMessageTypeByName(messageTypeName)!;

            try
            {
                var messageAsType = JsonSerializer.Deserialize(message.Body, messageType, new JsonSerializerOptions()
                {
                    PropertyNameCaseInsensitive = true,
                    AllowTrailingCommas = true
                });

                var handlerResponse = await _dispatcher.DispatchAsync((IMessage) messageAsType!);
                if (handlerResponse.Status == ResponseStatus.Successful)
                {
                    await _sqs.DeleteMessageAsync(queueUrl.QueueUrl, message.ReceiptHandle, stoppingToken);
                }
                else
                {
                    _logger.LogWarning(
                        "Failed to process the messageId {MessageMessageId} with error: {HandlerResponseMessage}",
                        message.MessageId, handlerResponse.Message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Fail to Deserialization the message : {MessageType}, {MessageBody}, error={Ex}",
                    messageType, message.Body, ex);
            }
        }
    }
}