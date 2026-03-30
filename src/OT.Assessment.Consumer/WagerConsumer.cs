using Dapper;
using MassTransit;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using OT.Assessment.Shared;
using System.Data;

namespace OT.Assessment.Consumer
{
    public class WagerConsumer(ILogger<WagerConsumer> logger, IServiceScopeFactory serviceScopeFactory) : IConsumer<Batch<CasinoWager>>
    {
        private const string _batchWagersSpName = "sp_ProcessWagerBatch";

        public async Task Consume(ConsumeContext<MassTransit.Batch<CasinoWager>> context)
        {
            try
            {
                var wagers = context.Message.Select(m => m.Message).ToList();
                logger.LogInformation("Ingesting batch of {Count} wagers...", wagers.Count);


                var scope = serviceScopeFactory.CreateScope();
                var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
                using var connection = new SqlConnection(config.GetConnectionString("OT-Assessment-DB"));

                var table = ConvertToDataTable(wagers);
                var wagersDataTable = table.AsTableValuedParameter("WagerTableType");
                var @params = new { Wagers = wagersDataTable };

                // Execute the TVP stored procedure
                await connection.ExecuteAsync(
                    _batchWagersSpName,
                    @params,
                    commandType: CommandType.StoredProcedure
                );

                logger.LogInformation("Batch of {Count} wagers saved successfully", wagers.Count);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing batch of wagers: {Message}", ex.Message);
                throw;
            }
        }

        private static DataTable ConvertToDataTable(IEnumerable<CasinoWager> wagers)
        {
            var table = new DataTable();
            table.Columns.Add("WagerId", typeof(Guid));
            table.Columns.Add("Theme", typeof(string));
            table.Columns.Add("Provider", typeof(string));
            table.Columns.Add("GameName", typeof(string));
            table.Columns.Add("TransactionId", typeof(string));
            table.Columns.Add("BrandId", typeof(Guid));
            table.Columns.Add("AccountId", typeof(Guid));
            table.Columns.Add("Username", typeof(string));
            table.Columns.Add("ExternalReferenceId", typeof(Guid));
            table.Columns.Add("TransactionTypeId", typeof(Guid));
            table.Columns.Add("Amount", typeof(decimal));
            table.Columns.Add("CreatedDateTime", typeof(DateTime));
            table.Columns.Add("NumberOfBets", typeof(int));
            table.Columns.Add("CountryCode", typeof(string));
            table.Columns.Add("SessionData", typeof(string));
            table.Columns.Add("Duration", typeof(long));

            foreach (var w in wagers)
            {
                // Convert strings to GUIDs - would change the model to use GUIDs but don't want to break the contract with the Bogus Tester
                table.Rows.Add(
                    Guid.Parse(w.WagerId),
                    w.Theme,
                    w.Provider,
                    w.GameName,
                    w.TransactionId,
                    Guid.Parse(w.BrandId),
                    Guid.Parse(w.AccountId),
                    w.Username,
                    Guid.Parse(w.ExternalReferenceId),
                    Guid.Parse(w.TransactionTypeId),
                    (decimal)w.Amount, // Cast double to decimal for SQL precision (need precision with financial transaction data)
                    w.CreatedDateTime,
                    w.NumberOfBets,
                    w.CountryCode,
                    w.SessionData,
                    w.Duration
                );
            }
            return table;
        }
    }
}
