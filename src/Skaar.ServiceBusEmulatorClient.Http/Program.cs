using Skaar.ServiceBusEmulatorClient.Http.Configuration;
using Skaar.ServiceBusEmulatorClient.Http.Extensions;
using Skaar.ServiceBusEmulatorClient.Http.Middleware;

var configBuilder = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional:true)
    .AddEnvironmentVariables()
    .AddCommandLine(args);
var configuration = configBuilder.Build();

var builder = WebApplication.CreateBuilder(args);


builder.Services.Configure<Settings>(configuration.GetSection("Settings"));
builder.Services.AddServices();
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.MapOpenApi();

app.UseHttpsRedirection();

app.MapEndpoints();

app.Run();