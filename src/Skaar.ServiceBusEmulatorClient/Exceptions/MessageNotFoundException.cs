namespace Skaar.ServiceBusEmulatorClient.Exceptions;

public class MessageNotFoundException(string queueName, string messageId)
    : ServiceBusEmulatorClientException($"Could not find message {queueName}/{messageId}")
{
    public string QueueName { get; } = queueName;
    public string MessageId { get; } = messageId;
}