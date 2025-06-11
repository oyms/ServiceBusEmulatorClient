using Skaar.ServiceBusEmulatorClient.Model;
using System.Text.Json;

namespace Skaar.ServiceBusEmulatorClient.Http.Models;

[System.Text.Json.Serialization.JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
public record MessageReference
{
    public MessageReference(QueueMessage message, QueueOrTopicName parentName, string link)
    {
        Id = message.Id;
        Subject = message.Subject;
        ContentType = message.ContentType;
        Parent = parentName;
        Href = link;
    }

    public string Href { get; }
    public QueueOrTopicName Parent { get; }
    public string ContentType { get; }
    public string Subject { get; }
    public MessageId Id { get; }
};