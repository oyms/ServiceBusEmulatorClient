using Azure.Messaging.ServiceBus;
using Skaar.ServiceBusEmulatorClient.Model;

namespace Skaar.ServiceBusEmulatorClient.Exceptions;

public class QueueNotFoundException(ServiceBusException inner, QueueOrTopicName queueName)
    : ServiceBusEmulatorClientException("Could not find queue " + queueName, inner)
{
    public QueueOrTopicName QueueName { get; } = queueName;
}