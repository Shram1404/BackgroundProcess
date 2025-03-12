namespace BackgroundService.Commands
{
    public interface IServiceCommand
    {
        Task<string> ExecuteAsync();
    }
}
