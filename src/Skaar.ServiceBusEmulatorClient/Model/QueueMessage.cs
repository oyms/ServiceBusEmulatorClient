using Azure.Messaging.ServiceBus;

namespace Skaar.ServiceBusEmulatorClient.Model;

public record QueueMessage
{
    private readonly ServiceBusReceivedMessage _message;

    public QueueMessage(ServiceBusReceivedMessage message)
    {
        _message = message;
    }
    
    public static explicit operator ServiceBusReceivedMessage(QueueMessage message) => message._message;
    
    public MessageId Id => MessageId.Parse(_message.MessageId);

    public string ContentType => _message.ContentType;
    public BinaryData Body => _message.Body;
    public override string ToString() => _message.ToString();
};