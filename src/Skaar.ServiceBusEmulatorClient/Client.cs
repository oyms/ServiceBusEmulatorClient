using System.Diagnostics.CodeAnalysis;
using Azure.Messaging.ServiceBus;
using Skaar.ServiceBusEmulatorClient.Model;

namespace Skaar.ServiceBusEmulatorClient;

public class Client(IConfiguration configuration):IAsyncDisposable
{
    private ServiceBusClient? _client;

    [MemberNotNull(nameof(_client))]
    private void SetUp()
    {
        _client ??= new ServiceBusClient(configuration.ConnectionString);
    }

    public async Task SendMessage(string queue, string contentType, ReadOnlyMemory<byte> data)
    {
        SetUp();
        await using var sender = _client.CreateSender(queue);
        var message = new ServiceBusMessage(data)
        {
            ContentType = contentType
        };
        await sender.SendMessageAsync(message);
    }

    public Task SendJsonMessage<T>(string queue, T body, System.Text.Json.JsonSerializerOptions? options = null)
    {
        var json = System.Text.Json.JsonSerializer.Serialize<T>(body, options);
        return SendJsonMessage(queue, json);
    }
    
    private async Task SendJsonMessage(string queue, string jsonBody)
    {
        SetUp();
        await using var sender = _client.CreateSender(queue);
        var message = new ServiceBusMessage(jsonBody)
        {
            ContentType = "application/json"
        };
        await sender.SendMessageAsync(message);
    }

    public async IAsyncEnumerable<QueueMessage> PeekAllMessages(string queue)
    {
        SetUp();
        await using var receiver = _client.CreateReceiver(queue);
        long? sequenceNumber = null;
        while (true)
        {
            IReadOnlyList<ServiceBusReceivedMessage> msgs;
            if (sequenceNumber.HasValue)
            {
                msgs = await receiver.PeekMessagesAsync(maxMessages:100, fromSequenceNumber:sequenceNumber.Value);
            }
            else
            {
                msgs = await receiver.PeekMessagesAsync(maxMessages:100);
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

    public async ValueTask DisposeAsync()
    {
        if (_client is not null)
        {
            await _client.DisposeAsync();
        }
    }
}