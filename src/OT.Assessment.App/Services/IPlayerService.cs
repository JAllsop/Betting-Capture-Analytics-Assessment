namespace OT.Assessment.App.Services
{
    public interface IPlayerService
    {
        Task<PagedResult<PlayerWagerResponse>> GetPlayerWagersAsync(Guid playerId, int page, int pageSize);

        Task<IEnumerable<PlayerStatResponse>> GetTopSpendersAsync(int count);

        Task ClearAllDataAsync();
    }
}
