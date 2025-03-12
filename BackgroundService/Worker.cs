using BackgroundService;
using BackgroundService.Commands;

namespace MyBackgroundService
{
    public class Worker : Microsoft.Extensions.Hosting.BackgroundService
    {
        const string ServiceName = "BackgroundWorkerService";
        private readonly ILogger<Worker> logger;

        public Worker(ILogger<Worker> logger)
        {
            this.logger = logger;

            //var command = ParseCommand("http https://google.com -execute-at-time 12.03.2025-14:05:40"); // TEST LOGIC
            //HandleCommandAsync(command, CancellationToken.None); // TEST LOGIC
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var server = NativePipeServer.CreatePipeServer(ServiceName))
                    {
                        await server.WaitForConnectionAsync(stoppingToken);

                        using (var reader = new StreamReader(server))
                        using (var writer = new StreamWriter(server) { AutoFlush = true })
                        {
                            string? request = await reader.ReadLineAsync();
                            if (!string.IsNullOrEmpty(request))
                            {
                                logger.LogInformation($"Received: {request}");

                                var command = ParseCommand(request);

                                string response = string.Empty;
                                if (command != null)
                                {
                                    response = await HandleCommandAsync(command, stoppingToken);
                                }

                                logger.LogInformation($"Command Response: {response}");

                                await writer.WriteLineAsync(response);
                            }
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error in background service execution.");
                }
            }
        }

        private IServiceCommand? ParseCommand(string request)
        {
            var parts = request.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length > 0)
            {
                var commandType = parts[0];

                var parameters = new Dictionary<string, string>();
                for (int i = 1; i < parts.Length; i++)
                {
                    if (parts[i].StartsWith("-"))
                    {
                        var key = parts[i].TrimStart('-');
                        var value = i + 1 < parts.Length && !parts[i + 1].StartsWith("-") ? parts[i + 1] : string.Empty;

                        parameters[key] = value;
                        if (!string.IsNullOrEmpty(value)) i++;
                    }
                    else if (i == 1)
                    {
                        parameters["url"] = parts[i];
                    }
                }

                IServiceCommand baseCommand = commandType switch // TODO: Fix to pass only parameters
                {
                    Arguments.SendHttpRequest => new HttpRequestCommand(parameters["url"]),
                    Arguments.ShowToast => new ToastCommand(),
                    Arguments.AppStart => new AppStartCommand(),
                    _ => throw new InvalidOperationException("Unknown command type")
                };

                if (parameters.ContainsKey(Arguments.ExecuteAtTime) || parameters.ContainsKey(Arguments.ExecuteEvery))
                {
                    return new TimedCommand(baseCommand, parameters);
                }

                return baseCommand;
            }

            return null;
        }



        private async Task<string> HandleCommandAsync(IServiceCommand command, CancellationToken stoppingToken)
        {
            if (command is TimedCommand timedCommand)
            {
                await timedCommand.StartAsync(); // Таймерна команда обробляє логіку часу
                return "Timed command scheduled.";
            }
            else
            {
                return await command.ExecuteAsync(); // Звичайна команда виконується одразу
            }
        }
    }
}