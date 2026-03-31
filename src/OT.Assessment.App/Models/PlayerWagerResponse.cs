namespace OT.Assessment.App.Models
{
    public record PlayerWagerResponse(
        Guid WagerId,
        string Game,
        string Provider,
        decimal Amount,
        DateTime CreatedDate
    );
}
