
using Skaar.ServiceBusEmulatorClient;
using Skaar.ServiceBusEmulatorClient.Model;
using System.Reflection;

namespace Skaar.ServiceBusEmulatorClientTests;

[Trait("Category", "ClientTests")]
public class ClientTests(ITestContextAccessor testContextAccessor)
{
    private readonly QueueOrTopicName _queueName = QueueOrTopicName.Parse("queue.1");
    private readonly IConfiguration _config = new EmulatorConfiguration();
    private readonly ITestOutputHelper _output = testContextAccessor.Current.TestOutputHelper!;
    
    [Fact]
    public async Task AddMessage()
    {
        await using var target = new Client(_config);
        var body = new{Text="Some text", Flag=true, Value=Random.Shared.Next()};
        await target.SendJsonMessage(_queueName, body, null, testContextAccessor.Current.CancellationToken);
    }    
    
    [Fact]
    public async Task AddBinaryMessage()
    {
        var cancellationToken = testContextAccessor.Current.CancellationToken;
        await using var image = Assembly.GetExecutingAssembly().GetManifestResourceStream("Skaar.ServiceBusEmulatorClientTests.Resources.sign.png");
        await using var target = new Client(_config);
        await using var stream = new MemoryStream();
        await image!.CopyToAsync(stream, cancellationToken);
        await target.SendMessage(_queueName, "image/png", stream.ToArray(), "Bus stop", cancellationToken);
    }
    
    [Fact]
    public async Task PeekMessages()
    {
        await using var target = new Client(_config);

        var result = target.PeekAllMessages(_queueName, testContextAccessor.Current.CancellationToken);

        await foreach (var msg in result)
        {
            _output.WriteLine(msg.ToString());
        }
    }
}