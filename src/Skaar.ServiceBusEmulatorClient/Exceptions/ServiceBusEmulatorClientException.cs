namespace Skaar.ServiceBusEmulatorClient.Exceptions;

public abstract class ServiceBusEmulatorClientException : Exception
{
    public ServiceBusEmulatorClientException() { }
    public ServiceBusEmulatorClientException(string message) : base(message) { }
    public ServiceBusEmulatorClientException(string message, Exception inner) : base(message, inner) { }
}