using Skaar.ServiceBusEmulatorClient.Http.Configuration;
using Skaar.ServiceBusEmulatorClient.Http.Extensions;
using Skaar.ServiceBusEmulatorClient.Http.Middleware;
using System.Reflection;

var configBuilder = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional:true)
    .AddEnvironmentVariables()
    .AddCommandLine(args);
var configuration = configBuilder.Build();

var builder = WebApplication.CreateBuilder(args);


builder.Services.Configure<Settings>(configuration.GetSection("Settings"));
builder.Services.AddServices();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApiDocument(config =>
{
    config.Title = "Servicebus emulator client";
    config.Description = "A simplified http interface to the servicebus client library";
    config.Version = Assembly.GetExecutingAssembly().GetName().Version?.ToString();
});

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.MapOpenApi();

app.UseHttpsRedirection();

app.MapEndpoints();

app.UseOpenApi();
app.UseSwaggerUi();

app.Run();