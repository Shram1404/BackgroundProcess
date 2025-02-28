using Microsoft.Extensions.Hosting;
using System.IO.Pipes;

namespace BackgroundService;

public class Worker : Microsoft.Extensions.Hosting.BackgroundService
{
    const string ServiceName = "MyTestBackgroundService"; // TODO: IMPORTANT - Must be unique for each service or application // TODO: Move to shared constants

    private readonly ILogger<Worker> _logger;

    public Worker(ILogger<Worker> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using (var server = new NamedPipeServerStream(ServiceName, PipeDirection.InOut, 1))
            {
                await server.WaitForConnectionAsync(stoppingToken);

                using (var reader = new StreamReader(server))
                using (var writer = new StreamWriter(server) { AutoFlush = true })
                {
                    string request = await reader.ReadLineAsync();
                    _logger.LogInformation($"Received: {request}");

                    string response = $"Processed: {request}";
                    await writer.WriteLineAsync(response);
                }
            }
        }
    }
}
