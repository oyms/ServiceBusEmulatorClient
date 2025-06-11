using Microsoft.AspNetCore.Http.HttpResults;
using Skaar.ServiceBusEmulatorClient.Model;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace Skaar.ServiceBusEmulatorClient.Http.Extensions;

public static class EndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/queue/{queueName}", GetMessagesFromQueue)
            .WithName("Peek all messages from queue")
            .Produces<IEnumerable<string>>(200)
            .Produces<NotFound>();

        endpoints.MapGet("/queue/{queueName}/ids", GetMessageIdsFromQueue)
            .WithName("Peek all message-ids from queue")
            .Produces<IEnumerable<string>>(200)
            .Produces<NotFound>();

        endpoints.MapGet("/queue/{queueName}/{messageId}", GetMessageFromQueue)
            .WithName("Peek at a message from queue")
            .Produces(200)
            .Produces<NotFound>();

        endpoints.MapDelete("/queue/{queueName}/{messageId}", CompleteMessageFromQueue)
            .WithName("Complete (remove) a message from queue")
            .Produces(200)
            .Produces<NotFound>();

        endpoints.MapPost("/queue/{queueOrTopicName}", PostMessage)
            .WithName("Put a message in the queue")
            .Produces(204)
            .Produces<NotFound>();
        
        endpoints.MapPost("/topic/{queueOrTopicName}", PostMessage)
            .WithName("Put a message in the topic")
            .Produces(204)
            .Produces<NotFound>();

        endpoints.MapGet("/topic/{topicName}/{subscription}", GetMessagesFromTopic)
            .WithName("Peek all messages from topic")
            .Produces<IEnumerable<string>>(200)
            .Produces<NotFound>();

        endpoints.MapGet("/topic/{topicName}/{subscription}/ids", GetMessageIdsFromTopic)
            .WithName("Peek all message-ids from topic")
            .Produces<IEnumerable<string>>(200)
            .Produces<NotFound>();

        endpoints.MapGet("/topic/{topicName}/{subscription}/{messageId}", GetMessageFromTopic)
            .WithName("Peek at a message from topic")
            .Produces(200)
            .Produces<NotFound>();

        endpoints.MapDelete("topic/{topicName}/{subscription}/{messageId}", CompleteMessageFromTopic)
            .WithName("Complete (remove) a message from topic")
            .Produces(200)
            .Produces<NotFound>();

        return endpoints;
    }

    private static async Task<IResult> PostMessage(QueueOrTopicName queueOrTopicName, IClient client,
        HttpContext context, CancellationToken ct)
    {
        var contentType = context.Request.ContentType;
        var subject = context.Request.Headers["subject"];
        var body = context.Request.Body;
        var length = context.Request.ContentLength.GetValueOrDefault(1024 * 4);
        if (string.IsNullOrWhiteSpace(contentType))
        {
            return Results.BadRequest("No content type was provided.");
        }

        var pool = MemoryPool<byte>.Shared;
        var rentedBuffer = pool.Rent((int)length);
        int totalBytesRead = 0;
        try
        {
            var memory = rentedBuffer.Memory;
            int bytesRead;
            while ((bytesRead = await body.ReadAsync(memory.Slice(totalBytesRead))) > 0)
            {
                totalBytesRead += bytesRead;

                if (totalBytesRead >= memory.Length)
                    throw new InvalidOperationException("Request body too large for buffer.");
            }

            await client.SendMessage(queueOrTopicName, contentType, memory.Slice(0, totalBytesRead), subject, ct);
        }
        finally
        {
            rentedBuffer.Dispose();
        }

        return Results.NoContent();
    }

    private static async IAsyncEnumerable<string> GetMessageIdsFromQueue(QueueOrTopicName queueName, IClient client,
        HttpContext context, LinkGenerator linker, [EnumeratorCancellation] CancellationToken ct)
    {
        var msgs = client.PeekAllMessages(queueName, ct);
        await foreach (var msg in msgs)
        {
            var link = linker.GetUriByName(context, "Peek at a message from queue",
                new { queueName, messageId = msg.Id })!;
            yield return link;
        }
    }

    private static async IAsyncEnumerable<string> GetMessageIdsFromTopic(QueueOrTopicName topicName,
        SubscriptionName subscription, IClient client, HttpContext context, LinkGenerator linker,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var msgs = client.PeekAllMessages(topicName, subscription, ct);
        await foreach (var msg in msgs)
        {
            var link = linker.GetUriByName(context, "Peek at a message from topic",
                new { topicName, messageId = msg.Id, subscription })!;
            yield return link;
        }
    }

    private static async Task GetMessagesFromQueue(QueueOrTopicName queueName, IClient client, HttpContext context,
        CancellationToken ct)
    {
        var msgs = client.PeekAllMessages(queueName, ct);
        await RenderMessagesAsMultiPart(msgs, context, ct);
    }

    private static async Task GetMessagesFromTopic(QueueOrTopicName topicName, SubscriptionName subscription,
        IClient client, HttpContext context, CancellationToken ct)
    {
        var msgs = client.PeekAllMessages(topicName, subscription, ct);
        await RenderMessagesAsMultiPart(msgs, context, ct);
    }

    private static async Task<IResult> GetMessageFromQueue(QueueOrTopicName queueName, MessageId messageId,
        IClient client, CancellationToken ct)
    {
        var msg = await client.PeekMessage(queueName, messageId, ct);
        var results = Results.Bytes(msg.Body, msg.ContentType);
        return results;
    }

    private static async Task<IResult> GetMessageFromTopic(QueueOrTopicName topicName, SubscriptionName subscription,
        MessageId messageId, IClient client, CancellationToken ct)
    {
        var msg = await client.PeekMessage(topicName, subscription, messageId, ct);
        var results = Results.Bytes(msg.Body, msg.ContentType);
        return results;
    }

    private static async Task<IResult> CompleteMessageFromQueue(QueueOrTopicName queueName, MessageId messageId,
        IClient client, CancellationToken ct)
    {
        await client.CompleteMessage(queueName, messageId, ct);
        return Results.NoContent();
    }

    private static async Task<IResult> CompleteMessageFromTopic(QueueOrTopicName topicName,
        SubscriptionName subscription, MessageId messageId, IClient client, CancellationToken ct)
    {
        await client.CompleteMessage(topicName, subscription, messageId, ct);
        return Results.NoContent();
    }

    private static async Task RenderMessagesAsMultiPart(IAsyncEnumerable<QueueMessage> msgs, HttpContext context,
        CancellationToken ct)
    {
        var boundary = "boundary_" + Guid.NewGuid();
        context.Response.ContentType = $"multipart/mixed; boundary={boundary}";

        await using var writer = new StreamWriter(context.Response.Body, leaveOpen: true);

        await foreach (var msg in msgs.WithCancellation(ct))
        {
            // --boundary line
            await writer.WriteAsync($"--{boundary}\r\n");

            // Content headers
            await writer.WriteAsync($"Content-Type: {msg.ContentType ?? "application/octet-stream"}\r\n");
            await writer.WriteAsync("Content-Disposition: inline\r\n\r\n");

            await writer.FlushAsync(ct);

            // Write binary data
            await context.Response.Body.WriteAsync(msg.Body, ct);
            await context.Response.Body.WriteAsync("\r\n"u8.ToArray(), ct);
            await context.Response.Body.FlushAsync(ct);
        }

        // Final boundary
        await writer.WriteAsync($"--{boundary}--\r\n");
        await writer.FlushAsync(ct);
    }
}