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
    
    public string Id => _message.MessageId;

    public override string ToString() => _message.ToString();
};