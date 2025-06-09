using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Options;
using Skaar.ServiceBusEmulatorClient.Http.Configuration;
using System.Buffers;

namespace Skaar.ServiceBusEmulatorClient.Http.Extensions;

public static class EndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/queue/{queueName}", GetMessages)
            .WithName("Peek all messages from queue")
            .Produces<IEnumerable<string>>(200)
            .Produces<NotFound>();   
        
        endpoints.MapGet("/queue/{queueName}/ids", GetMessageIds)
            .WithName("Peek all message-ids from queue")
            .Produces<IEnumerable<string>>(200)
            .Produces<NotFound>();       
        
        endpoints.MapGet("/queue/{queueName}/{messageId}", GetMessage)
            .WithName("Peek at a message from queue")
            .Produces(200)
            .Produces<NotFound>();        
        
        endpoints.MapPost("/queue/{queueName}", PostMessage)
            .WithName("Put a message in the queue")
            .Produces(204)
            .Produces<NotFound>();
        
        return endpoints;
    }

    private static async Task<IResult> PostMessage(string queueName, IClient client, HttpContext context, CancellationToken ct)
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

            await client.SendMessage(queueName, contentType, memory.Slice(0, totalBytesRead), subject, ct);
        }
        finally
        {
            rentedBuffer.Dispose();
        }

        return Results.NoContent();
    }
    
    private static async IAsyncEnumerable<string> GetMessageIds(string queueName, IClient client, HttpContext context, LinkGenerator linker, CancellationToken ct)
    {
        await foreach (var msg in client.PeekAllMessages(queueName, ct))
        {
            var link = linker.GetUriByName(context, "Peek at a message from queue", new { queueName, messageId = msg.Id })!;
            yield return link;
        }
    }

    private static async Task GetMessages(string queueName, IClient client, HttpContext context, CancellationToken ct)
    {
        var boundary = "boundary_" + Guid.NewGuid();
        context.Response.ContentType = $"multipart/mixed; boundary={boundary}";
    
        await using var writer = new StreamWriter(context.Response.Body, leaveOpen: true);

        await foreach (var msg in client.PeekAllMessages(queueName, ct))
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
    
    private static async Task<IResult> GetMessage(string queueName, string messageId, IClient client, CancellationToken ct)
    {
        var msg = await client.PeekMessage(queueName, messageId, ct);
        var results = Results.Bytes(msg.Body, msg.ContentType);
        return results;
    }
}

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.AddSingleton<IConfiguration>(svc => svc.GetRequiredService<IOptions<Settings>>().Value);
        services.AddSingleton<IClient, Client>();
        return services;
    }
}