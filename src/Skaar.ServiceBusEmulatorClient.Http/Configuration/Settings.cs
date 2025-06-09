namespace Skaar.ServiceBusEmulatorClient.Http.Configuration;

public record Settings: IConfiguration
{
    public required string ConnectionString { get; init; }
};