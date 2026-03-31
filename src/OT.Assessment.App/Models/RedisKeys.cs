namespace OT.Assessment.App.Models
{
    public static class RedisKeys
    {
        public const string PlayerLeaderboard = "player_leaderboard";

        public static string PlayerWagers(Guid playerId, int page, int pageSize) => $"player|{playerId}|wagers|p{page}|s{pageSize}";
    }
}