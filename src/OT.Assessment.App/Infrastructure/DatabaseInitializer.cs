using DbUp;
using Microsoft.Data.SqlClient;
using Polly;
using Polly.Retry;

namespace OT.Assessment.App.Infrastructure;

public static class DatabaseInitializer
{
    public static async Task InitializeDatabase(IConfiguration config, ILogger logger)
    {
        var targetConString = config.GetConnectionString("OT-Assessment-DB");
        // needed for when the DB has not been created so we need a generic none DB specific con string
        var conBuilder = new SqlConnectionStringBuilder(targetConString)
        {
            InitialCatalog = string.Empty,
            ConnectTimeout = 30
        };
        var genericConString = conBuilder.ConnectionString;

        var scriptPath = Path.Combine(AppContext.BaseDirectory, "Infrastructure", "DatabaseGenerate.sql");
        if (!File.Exists(scriptPath))
        {
            logger.LogError("Database script not found at {path}", scriptPath);
            return;
        }

        string scriptContent = null;
        try
        {
            scriptContent = await File.ReadAllTextAsync(scriptPath);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to read database script at {path}", scriptPath);
            return;
        }

        // Polly pipeline - Retry with exponential backoff
        var pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder().Handle<Exception>(), // Handle SqlException, Win32Exception, etc.
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true, // Prevents multiple instances from retrying in sync
                MaxRetryAttempts = 5,
                Delay = TimeSpan.FromSeconds(3),
                OnRetry = args =>
                {
                    logger.LogWarning(args.Outcome.Exception, "SQL Server not ready - retrying in {delay}... (Attempt {attempt})", args.RetryDelay, args.AttemptNumber);
                    return default;
                }
            })
            .Build();

        await pipeline.ExecuteAsync(async token =>
        {
            using (var masterConn = new SqlConnection(genericConString))
            {
                // Needed for Docker version
                EnsureDatabase.For.SqlDatabase(targetConString);
                logger.LogInformation("Database 'OT-Assessment-DB' found/created");
            }

            // This will trigger Polly if the DB is still "Starting Up"
            using (var targetConn = new SqlConnection(targetConString))
            {
                await targetConn.OpenAsync(token);
            }

            var upgrader = DeployChanges.To
                .SqlDatabase(targetConString)
                // Note: DbUp won't re-run this if the name "DatabaseGenerate" already exists in the SchemaVersions
                .WithScript("DatabaseGenerate", scriptContent)
                .LogTo(new DbUpLogger(logger))
                .LogScriptOutput()
                .Build();

            var result = upgrader.PerformUpgrade();

            if (!result.Successful)
            {
                logger.LogError(result.Error, "Database 'OT-Assessment-DB' upgrade failed!");
                throw result.Error;
            }

            logger.LogInformation("Database 'OT-Assessment-DB' initialised and ready");
        });
    }

#pragma warning disable CA2254 // Template should be a static expression
    private class DbUpLogger(ILogger logger) : DbUp.Engine.Output.IUpgradeLog
    {
        public void WriteError(string format, params object[] args) => logger.LogError(format, args);
        public void WriteInformation(string format, params object[] args) => logger.LogInformation(format, args);
        public void WriteWarning(string format, params object[] args) => logger.LogWarning(format, args);

        public void LogTrace(string format, params object[] args) => logger.LogTrace(format, args);
        public void LogDebug(string format, params object[] args) => logger.LogDebug(format, args);
        public void LogInformation(string format, params object[] args) => logger.LogInformation(format, args);
        public void LogWarning(string format, params object[] args) => logger.LogWarning(format, args);
        public void LogError(string format, params object[] args) => logger.LogError(format, args);
        public void LogError(Exception ex, string format, params object[] args) => logger.LogError(ex, format, args);
    }
#pragma warning restore CA2254
}