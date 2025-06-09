using System.Diagnostics.CodeAnalysis;
using Azure.Messaging.ServiceBus;
using Skaar.ServiceBusEmulatorClient.Exceptions;
using Skaar.ServiceBusEmulatorClient.Model;
using System.Runtime.CompilerServices;

namespace Skaar.ServiceBusEmulatorClient;

public class Client(IConfiguration configuration):IAsyncDisposable, IClient
{
    private ServiceBusClient? _client;

    [MemberNotNull(nameof(_client))]
    private void SetUp()
    {
        _client ??= new ServiceBusClient(configuration.ConnectionString);
    }

    public async Task SendMessage(string queue, string contentType, ReadOnlyMemory<byte> data, string? subject = null, CancellationToken ct = default)
    {
        SetUp();
        await using var sender = _client.CreateSender(queue);
        var message = new ServiceBusMessage(data)
        {
            ContentType = contentType,
            Subject = subject
        };
        await sender.SendMessageAsync(message, ct);
    }

    public Task SendJsonMessage<T>(string queue, T body, System.Text.Json.JsonSerializerOptions? options = null, CancellationToken ct = default)
    {
        var json = System.Text.Json.JsonSerializer.Serialize<T>(body, options);
        return SendJsonMessage(queue, json, ct);
    }
    
    private async Task SendJsonMessage(string queue, string jsonBody, CancellationToken ct = default)
    {
        SetUp();
        await using var sender = _client.CreateSender(queue);
        var message = new ServiceBusMessage(jsonBody)
        {
            ContentType = "application/json"
        };
        await sender.SendMessageAsync(message, ct);
    }

    public async IAsyncEnumerable<QueueMessage> PeekAllMessages(
        string queue, 
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        SetUp();
        await using var receiver = _client.CreateReceiver(queue);
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
                catch (ServiceBusException e) when(e.Reason == ServiceBusFailureReason.MessagingEntityNotFound)
                {
                    throw new QueueNotFoundException(e, queue);
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

    public async Task<QueueMessage> PeekMessage(string queue, string messageId, CancellationToken ct = default)
    {
        SetUp();
        await using var receiver = _client.CreateReceiver(queue);
        await foreach (var msg in PeekAllMessages(queue, ct))
        {
            if (msg.Id == messageId) return msg;
        }
        throw new MessageNotFoundException(queue, messageId);
    }

    public async ValueTask DisposeAsync()
    {
        if (_client is not null)
        {
            await _client.DisposeAsync();
        }
    }
}