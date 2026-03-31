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
            table.Columns.Add("TransactionId", typeof(Guid));
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
                table.Rows.Add(
                    w.WagerId,
                    w.Theme,
                    w.Provider,
                    w.GameName,
                    w.TransactionId,
                    w.BrandId,
                    w.AccountId,
                    w.Username,
                    w.ExternalReferenceId,
                    w.TransactionTypeId,
                    w.Amount,
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
