using AutoMapper;

namespace AccountSyncData.Consumer.Queue;

public class MessageMappings : Profile
{
    public MessageMappings()
    {
        CreateMap<Amazon.SQS.Model.Message, Message>();
        CreateMap<Amazon.SQS.Model.MessageAttributeValue, MessageAttribute>();
    }
}