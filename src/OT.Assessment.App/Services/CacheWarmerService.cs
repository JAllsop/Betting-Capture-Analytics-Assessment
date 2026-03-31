using OT.Assessment.App.Data;
using Polly;
using Polly.Retry;
using StackExchange.Redis;

namespace OT.Assessment.App.Services
{
    public class CacheWarmerService(IServiceScopeFactory scopeFactory, IConnectionMultiplexer redis, ILogger<CacheWarmerService> logger) : BackgroundService
    {
        private const int _numInitPlayersToCache = 1000;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var pipeline = new ResiliencePipelineBuilder()
                .AddRetry(new RetryStrategyOptions
                {
                    ShouldHandle = new PredicateBuilder().Handle<Exception>(),
                    BackoffType = DelayBackoffType.Exponential,
                    // would enable on a real implementation to avoid thundering herd issues, but for demo purposes it just delays the start up
                    //UseJitter = true, 
                    MaxRetryAttempts = 5,
                    Delay = TimeSpan.FromSeconds(3),
                    OnRetry = args =>
                    {
                        logger.LogWarning(args.Outcome.Exception,
                            "Cache Warmer: DB/Redis not ready - retrying in {delay}... (Attempt {attempt})",
                            args.RetryDelay,
                            args.AttemptNumber
                        );
                        return default;
                    }
                })
                .Build();

            await pipeline.ExecuteAsync(async token =>
            {
                logger.LogInformation("Cache Warmer: Populating Redis from SQL...");

                using var scope = scopeFactory.CreateScope();
                var repo = scope.ServiceProvider.GetRequiredService<IPlayerRepository>();
                var db = redis.GetDatabase();

                var topSpenders = await repo.GetTopSpendersAsync(_numInitPlayersToCache);

                var entries = topSpenders.Select(s =>
                    new SortedSetEntry($"{s.AccountId}:{s.Username}:{s.LastUpdated:o}", (double)s.TotalSpend)
                ).ToArray();

                if (entries.Length == 0)
                {
                    logger.LogInformation("Cache Warmer: No Players to Populate Cache");
                    return;
                }

                await db.SortedSetAddAsync(RedisKeys.PlayerLeaderboard, entries);
                logger.LogInformation("Cache Warmer: Successfully Populated {Count} Players", entries.Length);
            }, stoppingToken);
        }
    }
}
