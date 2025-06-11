using Skaar.ServiceBusEmulatorClient.Model;

namespace Skaar.ServiceBusEmulatorClient.Exceptions;

public class MessageNotFoundException(QueueOrTopicName queueName, MessageId messageId)
    : ServiceBusEmulatorClientException($"Could not find message {queueName}/{messageId}")
{
    public QueueOrTopicName QueueName { get; } = queueName;
    public MessageId MessageId { get; } = messageId;
}