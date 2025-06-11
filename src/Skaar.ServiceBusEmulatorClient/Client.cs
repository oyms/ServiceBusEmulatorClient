using System.Diagnostics.CodeAnalysis;
using Azure.Messaging.ServiceBus;
using Skaar.ServiceBusEmulatorClient.Exceptions;
using Skaar.ServiceBusEmulatorClient.Model;
using System.Runtime.CompilerServices;

namespace Skaar.ServiceBusEmulatorClient;

public class Client(IConfiguration configuration) : IAsyncDisposable, IClient
{
    private ServiceBusClient? _client;

    public async Task SendMessage(QueueOrTopicName queue, string contentType, ReadOnlyMemory<byte> data,
        string? subject = null, CancellationToken ct = default)
    {
        SetUp();
        await using var sender = _client.CreateSender(queue.ToString());
        var message = new ServiceBusMessage(data) { ContentType = contentType, Subject = subject };
        await sender.SendMessageAsync(message, ct);
    }

    public Task SendJsonMessage<T>(QueueOrTopicName queue, T body,
        System.Text.Json.JsonSerializerOptions? options = null, CancellationToken ct = default)
    {
        var json = System.Text.Json.JsonSerializer.Serialize<T>(body, options);
        return SendJsonMessage(queue, json, ct);
    }

    public async Task CompleteMessage(QueueOrTopicName queue, MessageId id, CancellationToken ct = default)
    {
        SetUp();
        await PeekMessage(queue, id, ct);
        await using var receiver = _client.CreateReceiver(queue.ToString(),
            new ServiceBusReceiverOptions { ReceiveMode = ServiceBusReceiveMode.PeekLock });
        if (!await CompleteMessage(receiver, id, ct))
        {
            throw new MessageNotFoundException(queue, id);
        };
    }

    public async Task CompleteMessage(QueueOrTopicName topic, SubscriptionName subscription, MessageId id,
        CancellationToken ct = default)
    {
        SetUp();
        await PeekMessage(topic, subscription, id, ct);
        await using var receiver = _client.CreateReceiver(topic.ToString(), subscription.ToString(),
            new ServiceBusReceiverOptions { ReceiveMode = ServiceBusReceiveMode.PeekLock });
        if (!await CompleteMessage(receiver, id, ct))
        {
            throw new MessageNotFoundException(topic, id);
        };
    }

    private async Task<bool> CompleteMessage(ServiceBusReceiver receiver, MessageId id, CancellationToken ct)
    {
        var receivedMessage = await receiver.ReceiveMessageAsync(maxWaitTime: TimeSpan.FromSeconds(3), cancellationToken: ct);
        while (receivedMessage != null)
        {
            if (receivedMessage.MessageId == id.ToString())
            {
                await receiver.CompleteMessageAsync(receivedMessage, ct);
                return true;
            }

            await receiver.AbandonMessageAsync(receivedMessage, cancellationToken: ct);
            receivedMessage = await receiver.ReceiveMessageAsync(cancellationToken: ct);
        }

        return false;
    }

    public IAsyncEnumerable<QueueMessage> PeekAllMessages(QueueOrTopicName queue, CancellationToken ct = default) =>
        PeekAllMessagesFromQueueOrTopicSubscription(queue, null, ct);

    public IAsyncEnumerable<QueueMessage> PeekAllMessages(QueueOrTopicName topic, SubscriptionName subscription,
        CancellationToken ct = default) => PeekAllMessagesFromQueueOrTopicSubscription(topic, subscription, ct);

    private async Task SendJsonMessage(QueueOrTopicName queue, string jsonBody, CancellationToken ct = default)
    {
        SetUp();
        await using var sender = _client.CreateSender(queue.ToString());
        var message = new ServiceBusMessage(jsonBody) { ContentType = "application/json" };
        await sender.SendMessageAsync(message, ct);
    }

    private async IAsyncEnumerable<QueueMessage> PeekAllMessagesFromQueueOrTopicSubscription(
        QueueOrTopicName queueOrTopicName, SubscriptionName? subscription,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        SetUp();
        await using var receiver = CreateReceiver(queueOrTopicName, subscription);
        long? sequenceNumber = null;
        while (true)
        {
            IReadOnlyList<ServiceBusReceivedMessage> msgs;
            try
            {
                if (sequenceNumber.HasValue)
                {
                    msgs = await receiver.PeekMessagesAsync(maxMessages: 100,
                        fromSequenceNumber: sequenceNumber.Value, ct);
                }
                else
                {
                    msgs = await receiver.PeekMessagesAsync(maxMessages: 100, cancellationToken: ct);
                }
            }
            catch (ServiceBusException e) when (e.Reason == ServiceBusFailureReason.MessagingEntityNotFound)
            {
                throw new QueueNotFoundException(e, queueOrTopicName);
            }

            if (!msgs.Any())
            {
                yield break;
            }

            foreach (var message in msgs)
            {
                yield return new QueueMessage(message);
            }

            sequenceNumber = msgs.Last().SequenceNumber + 1;
        }
    }

    public async Task<QueueMessage> PeekMessage(QueueOrTopicName queue, MessageId messageId,
        CancellationToken ct = default)
    {
        SetUp();
        await foreach (var msg in PeekAllMessages(queue, ct))
        {
            if (msg.Id == messageId) return msg;
        }

        throw new MessageNotFoundException(queue, messageId);
    }

    public async Task<QueueMessage> PeekMessage(QueueOrTopicName topic, SubscriptionName subscription,
        MessageId messageId, CancellationToken ct = default)
    {
        SetUp();
        await foreach (var msg in PeekAllMessages(topic, subscription, ct))
        {
            if (msg.Id == messageId) return msg;
        }

        throw new MessageNotFoundException(topic, messageId);
    }

    private ServiceBusReceiver CreateReceiver(QueueOrTopicName queueOrTopicName, SubscriptionName? subscription)
    {
        SetUp();
        if (subscription is null)
        {
            return _client.CreateReceiver(queueOrTopicName.ToString());
        }
        return _client.CreateReceiver(queueOrTopicName.ToString(), subscription.ToString());
    }

    [MemberNotNull(nameof(_client))]
    private void SetUp()
    {
        _client ??= new ServiceBusClient(configuration.ConnectionString);
    }

    public async ValueTask DisposeAsync()
    {
        if (_client is not null)
        {
            await _client.DisposeAsync();
        }
    }
}