namespace OT.Assessment.App.Models
{
    public record PlayerStatResponse(
        Guid AccountId,
        string Username,
        decimal TotalSpend,
        DateTime LastUpdated
    );
}
