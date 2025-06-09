using Azure.Messaging.ServiceBus;

namespace Skaar.ServiceBusEmulatorClient.Exceptions;

public class QueueNotFoundException(ServiceBusException inner, string queueName)
    : ServiceBusEmulatorClientException("Could not find queue " + queueName, inner)
{
    public string QueueName { get; } = queueName;
}