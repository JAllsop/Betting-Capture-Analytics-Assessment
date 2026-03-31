using Dapper;
using System.Data;

namespace OT.Assessment.App.Data
{
    public class PlayerRepository(IDbConnection db) : IPlayerRepository
    {
        public async Task<(IEnumerable<PlayerWagerResponse> Data, long Total)> GetPlayerWagersAsync(Guid playerId, int page, int pageSize)
        {
            var offset = (page - 1) * pageSize;

            const string sql = @"
            SELECT COUNT(*) FROM Wagers WHERE AccountId = @playerId;

            SELECT 
                WagerId, 
                GameName AS Game, 
                Provider, Amount, 
                CreatedDateTime AS CreatedDate
            FROM Wagers
            WHERE AccountId = @playerId
            ORDER BY CreatedDateTime DESC
            OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY;";

            using var multi = await db.QueryMultipleAsync(sql, new { playerId, offset, pageSize });
            var total = await multi.ReadFirstAsync<long>();
            var data = await multi.ReadAsync<PlayerWagerResponse>();

            return (data, total);
        }

        public async Task<IEnumerable<PlayerStatResponse>> GetTopSpendersAsync(int count)
        {
            const string sql = @"
            SELECT TOP (@count) 
                AccountId, 
                Username, 
                TotalSpend,
                LastUpdated
            FROM PlayerSpendStats
            ORDER BY TotalSpend DESC";

            return await db.QueryAsync<PlayerStatResponse>(sql, new { count });
        }

        public async Task ClearDataAsync()
        {
            const string sql = @"
            TRUNCATE TABLE Wagers;
            TRUNCATE TABLE PlayerSpendStats;";

            await db.ExecuteAsync(sql);
        }
    }
}
