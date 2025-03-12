namespace BackgroundService.Commands
{
    public class TimedCommand : IServiceCommand
    {
        private readonly IServiceCommand baseCommand;
        private readonly TimeSpan? interval;
        private readonly DateTime? startAtTime;
        private Timer? timer;

        public TimedCommand(IServiceCommand baseCommand, Dictionary<string, string> parameters)
        {
            this.baseCommand = baseCommand;

            if (parameters.ContainsKey(Arguments.ExecuteEvery))
            {
                interval = TimeSpan.FromMilliseconds(int.Parse(parameters[Arguments.ExecuteEvery]));
            }

            if (parameters.ContainsKey(Arguments.ExecuteAtTime))
            {
                startAtTime = DateTime.ParseExact(parameters[Arguments.ExecuteAtTime], "dd.MM.yyyy-HH:mm:ss", null);
            }
        }

        public async Task<string> ExecuteAsync()
        {
            return await baseCommand.ExecuteAsync();
        }

        public async Task StartAsync()
        {
            if (startAtTime.HasValue && DateTime.Now < startAtTime.Value)
            {
                var delayUntilStart = startAtTime.Value - DateTime.Now;
                await Task.Delay(delayUntilStart);
            }

            if (interval.HasValue)
            {
                timer = new Timer(async _ =>
                {
                    await baseCommand.ExecuteAsync();
                }, null, TimeSpan.Zero, interval.Value);
            }
            else
            {
                await baseCommand.ExecuteAsync();
            }
        }

        public void Stop()
        {
            timer?.Dispose();
            timer = null;
        }
    }
}
