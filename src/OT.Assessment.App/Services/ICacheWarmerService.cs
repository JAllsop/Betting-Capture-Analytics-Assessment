namespace OT.Assessment.App.Services
{
    public interface ICacheWarmerService
    {
        Task ExecuteAsync(CancellationToken stoppingToken);
    }
}
