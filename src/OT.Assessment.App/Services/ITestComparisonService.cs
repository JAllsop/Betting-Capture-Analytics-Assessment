namespace OT.Assessment.App.Services
{
    public interface ITestComparisonService
    {
        Task<string> GenerateComparisonReport();
    }
}
