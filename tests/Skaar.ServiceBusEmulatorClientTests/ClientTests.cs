
using Skaar.ServiceBusEmulatorClient;

namespace Skaar.ServiceBusEmulatorClientTests;

public class ClientTests(ITestContextAccessor testContextAccessor)
{
    private const string QueueName = "queue.1";
    private readonly IConfiguration _config = new EmulatorConfiguration();
    private readonly ITestOutputHelper _output = testContextAccessor.Current.TestOutputHelper!;
    
    [Fact]
    public async Task AddMessage()
    {
        await using var target = new Client(_config);
        var body = new{Text="Some text", Flag=true, Value=Random.Shared.Next()};
        await target.SendJsonMessage(QueueName, body);
    }
    
    [Fact]
    public async Task PeekMessages()
    {
        await using var target = new Client(_config);

        var result = target.PeekAllMessages(QueueName);

        await foreach (var msg in result)
        {
            _output.WriteLine(msg.ToString());
        }
    }
}