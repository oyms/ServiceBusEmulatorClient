using Skaar.ServiceBusEmulatorClient.Model;

namespace Skaar.ServiceBusEmulatorClient;

public interface IClient
{
    Task SendMessage(string queue, string contentType, ReadOnlyMemory<byte> data, string? subject = null, CancellationToken ct = default);
    Task SendJsonMessage<T>(string queue, T body, System.Text.Json.JsonSerializerOptions? options = null, CancellationToken ct = default);
    IAsyncEnumerable<QueueMessage> PeekAllMessages(string queue, CancellationToken ct = default);
    Task<QueueMessage> PeekMessage(string queue, string messageId, CancellationToken ct = default);
}