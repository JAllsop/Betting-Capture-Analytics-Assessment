namespace OT.Assessment.App.Data
{
    public interface IPlayerRepository
    {
        Task<(IEnumerable<PlayerWagerResponse> Data, long Total)> GetPlayerWagersAsync(Guid playerId, int page, int pageSize);

        Task<IEnumerable<PlayerStatResponse>> GetTopSpendersAsync(int count);

        Task ClearDataAsync();
    }
}
