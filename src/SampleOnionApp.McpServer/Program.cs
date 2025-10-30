using Microsoft.Extensions.DependencyInjection;
using SampleOnionApp.Application.Extensions;
using SampleOnionApp.Infrastructure.Extensions;
using SampleOnionApp.McpServer.Server;

var services = new ServiceCollection();
services.AddApplication();
services.AddInfrastructure();
services.AddSingleton<McpServerLoop>();

await using ServiceProvider provider = services.BuildServiceProvider();

using CancellationTokenSource cts = new();
Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    cts.Cancel();
};

var server = provider.GetRequiredService<McpServerLoop>();

try
{
    await server.RunAsync(cts.Token);
}
catch (OperationCanceledException)
{
    // Graceful shutdown requested.
}
