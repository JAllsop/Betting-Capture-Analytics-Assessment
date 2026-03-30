namespace OT.Assessment.App.Models
{
    public class PlayerStat
    {
        public Guid AccountId { get; set; }

        public string Username { get; set; } = default!;

        public double TotalSpend { get; set; }

        public DateTime LastUpdated { get; set; }
    }
}
