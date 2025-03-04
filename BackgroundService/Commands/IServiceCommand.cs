namespace BackgroundService.Commands
{
    internal interface IServiceCommand
    {
        Task<string> ExecuteAsync();
    }
}
