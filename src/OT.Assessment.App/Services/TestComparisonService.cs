using Dapper;
using Microsoft.Extensions.Logging;
using OT.Assessment.App.Models;
using OT.Assessment.Shared;
using System.Data;
using System.Text;
using System.Text.Json;

namespace OT.Assessment.App.Services
{
    public class TestComparisonService(IDbConnection db, ILogger<TestComparisonService> logger) : ITestComparisonService
    {
        private readonly string _resultsDir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "data_audit");

        public async Task<string> GenerateComparisonReport()
        {
            logger.LogInformation("Starting 3-way wager comparison report (Sent vs Received vs DB)...");

            var sentFilePath = Path.Combine(_resultsDir, "sent_wagers_audit.json");
            var sentWagers = File.Exists(sentFilePath)
                ? JsonSerializer.Deserialize<List<CasinoWager>>(await File.ReadAllTextAsync(sentFilePath)) ?? []
                : [];

            var receivedFilePath = Path.Combine(_resultsDir, "received_wagers_audit.json");
            var receivedWagers = File.Exists(receivedFilePath)
                ? JsonSerializer.Deserialize<List<CasinoWager>>(await File.ReadAllTextAsync(receivedFilePath)) ?? []
                : [];

            var dbWagers = (await db.QueryAsync<CasinoWager>("SELECT * FROM Wagers")).ToList();
            var dbPlayerStats = (await db.QueryAsync<PlayerStatResponse>("SELECT * FROM PlayerSpendStats")).ToList();

            // Get only unique wagers from the source file for the 'Expected' values
            var uniqueSentWagers = sentWagers
                .GroupBy(w => w.WagerId)
                .Select(g => g.First())
                .ToList();

            // Unique ID Analysis
            var sentIds = sentWagers.Select(w => w.WagerId).Distinct().ToHashSet();
            var receivedIds = receivedWagers.Select(w => w.WagerId).Distinct().ToHashSet();
            var dbWagerIds = dbWagers.Select(w => w.WagerId).ToHashSet();

            // Duplicate Analysis
            var receivedDuplicates = receivedWagers.Count - receivedIds.Count;
            var sentDuplicates = sentWagers.Count - sentIds.Count;

            // Integrity Logic
            var uniqueIdsLostInTransit = sentIds.Where(id => !receivedIds.Contains(id)).ToList();
            var uniqueIdsLostInQueue = receivedIds.Where(id => !dbWagerIds.Contains(id)).ToList();

            var report = new StringBuilder();
            report.AppendLine("Wager 3-Way Integrity Report");
            report.AppendLine("============================");
            report.AppendLine($"1. Total Wagers Sent (Tester):  {sentWagers.Count} (Unique: {sentIds.Count})");
            report.AppendLine($"2. Total Wagers Received (API): {receivedWagers.Count} (Unique: {receivedIds.Count})");
            report.AppendLine($"3. Total Wagers Saved (DB):     {dbWagers.Count}");
            report.AppendLine("----------------------------");

            // Analysis Section
            if (uniqueIdsLostInTransit.Count != 0)
            {
                report.AppendLine($"[FAIL] Network Loss: {uniqueIdsLostInTransit.Count} unique wagers never reached the API");
                report.AppendLine($"       Note: API received {receivedDuplicates} duplicate IDs (likely retries)");
            }
            else
            {
                report.AppendLine("[PASS] Network: All unique wagers successfully reached the API");
            }

            if (uniqueIdsLostInQueue.Count != 0)
            {
                report.AppendLine($"[CRITICAL] Persistence Loss: {uniqueIdsLostInQueue.Count} unique wagers reached API but are missing from DB");
            }
            else if (receivedIds.Count == dbWagers.Count)
            {
                report.AppendLine("[PASS] Persistence: DB count matches unique API receipts (Deduplication successful)");
            }

            report.AppendLine();
            report.AppendLine("Top 10 Player Stats Comparison (DB vs Sent Unique Source):");
            report.AppendLine("(Duplicates have been removed from Sent Source for accurate comparison)\n");

            // Calculate Stats using only the Deduplicated Sent Wagers
            var sentStats = uniqueSentWagers
                .GroupBy(w => w.AccountId)
                .Select(g => new { AccountId = g.Key, Total = g.Sum(w => w.Amount), Count = g.Count() })
                .ToDictionary(k => k.AccountId, v => v);

            foreach (var dbStat in dbPlayerStats.OrderByDescending(x => x.TotalSpend).Take(10))
            {
                sentStats.TryGetValue(dbStat.AccountId, out var sent);
                var diff = dbStat.TotalSpend - (sent?.Total ?? 0);
                var isMatch = Math.Abs(diff) < (decimal)0.05;

                report.AppendLine($"Player {dbStat.Username} ({dbStat.AccountId}):");
                report.AppendLine($"  - DB Spend: {dbStat.TotalSpend:N2} | Sent Source: {sent?.Total ?? 0:N2}");
                report.AppendLine($"  - Diff: {diff:N2} ({(isMatch ? "MATCH" : "MISMATCH - Check Network Loss")})\n");
            }

            return report.ToString();
        }
    }
}