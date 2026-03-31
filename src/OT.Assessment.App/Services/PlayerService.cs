using OT.Assessment.App.Data;
using StackExchange.Redis;
using System.Text.Json;

namespace OT.Assessment.App.Services
{
    public class PlayerService(IPlayerRepository repository, IConnectionMultiplexer redis, ILogger<PlayerService> logger) : IPlayerService
    {
        private readonly IDatabase _cache = redis.GetDatabase();

        public async Task<PagedResult<PlayerWagerResponse>> GetPlayerWagersAsync(Guid playerId, int page, int pageSize)
        {
            var cacheKey = RedisKeys.PlayerWagers(playerId, page, pageSize);
            var cachedData = await _cache.StringGetAsync(cacheKey);

            if (cachedData.HasValue)
            {
                logger.LogInformation("Cache hit for player wagers - returning cached data");
                return JsonSerializer.Deserialize<PagedResult<PlayerWagerResponse>>(cachedData!)!;
            }

            logger.LogInformation("Cache miss for player wagers - fetching from DB");
            var (data, total) = await repository.GetPlayerWagersAsync(playerId, page, pageSize);

            var totalPages = (int)Math.Ceiling((double)total / pageSize);
            var result = new PagedResult<PlayerWagerResponse>(data, page, pageSize, total, totalPages);

            // Cache the result for 1 minute (short TTL - high-frequency)
            await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), TimeSpan.FromMinutes(1));

            return result;
        }

        public async Task<IEnumerable<PlayerStatResponse>> GetTopSpendersAsync(int count)
        {
            var members = await _cache.SortedSetRangeByRankWithScoresAsync(RedisKeys.PlayerLeaderboard, 0, count - 1, Order.Descending);

            if (members.Length > 0)
            {
                logger.LogInformation("Leaderboard cache hit - returning cached data");
                return members.Select(m =>
                {
                    var parts = m.Element.ToString().Split('|');
                    var lastUpdated = DateTime.Parse(parts[2]);
                    return new PlayerStatResponse(Guid.Parse(parts[0]), parts[1], (decimal)m.Score, lastUpdated);
                });
            }

            logger.LogInformation("Leaderboard cache miss - fetching from DB");
            var topSpenders = await repository.GetTopSpendersAsync(count);

            foreach (var spender in topSpenders)
            {
                var cacheValue = $"{spender.AccountId}|{spender.Username}|{spender.LastUpdated:o}";
                await _cache.SortedSetAddAsync(RedisKeys.PlayerLeaderboard, cacheValue, (double)spender.TotalSpend);
            }

            return topSpenders;
        }

        public async Task ClearAllDataAsync()
        {
            await repository.ClearDataAsync();
            await _cache.ExecuteAsync("FLUSHDB"); // Clear all cached data
            logger.LogInformation("Cleared all data and invalidated cache");
        }
    }
}
