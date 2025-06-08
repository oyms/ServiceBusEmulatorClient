
namespace Skaar.ServiceBusEmulatorClientTests;

public class EmulatorConfiguration: Skaar.ServiceBusEmulatorClient.IConfiguration
{
    public string ConnectionString =>
        "Endpoint=sb://localhost;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;";
}