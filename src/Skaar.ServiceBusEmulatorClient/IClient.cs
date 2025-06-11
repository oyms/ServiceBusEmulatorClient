using Skaar.ServiceBusEmulatorClient.Model;

namespace Skaar.ServiceBusEmulatorClient;

public interface IClient
{
    Task SendMessage(QueueOrTopicName queue, string contentType, ReadOnlyMemory<byte> data, string? subject = null, CancellationToken ct = default);
    Task SendJsonMessage<T>(QueueOrTopicName queue, T body, System.Text.Json.JsonSerializerOptions? options = null, CancellationToken ct = default);
    Task<bool> CompleteMessage(QueueOrTopicName queue, MessageId id, CancellationToken ct = default);
    Task<bool> CompleteMessage(QueueOrTopicName topic, SubscriptionName subscription, MessageId id, CancellationToken ct = default);
    IAsyncEnumerable<QueueMessage> PeekAllMessages(QueueOrTopicName queue, CancellationToken ct = default);
    IAsyncEnumerable<QueueMessage> PeekAllMessages(QueueOrTopicName topic, SubscriptionName subscription, CancellationToken ct = default);
    Task<QueueMessage> PeekMessage(QueueOrTopicName queue, MessageId messageId, CancellationToken ct = default);
    Task<QueueMessage> PeekMessage(QueueOrTopicName topic, SubscriptionName subscription, MessageId messageId, CancellationToken ct = default);
}