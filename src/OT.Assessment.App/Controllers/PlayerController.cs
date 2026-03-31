using System.Collections.Concurrent;
using System.Text.Json;
using MassTransit;
using Microsoft.AspNetCore.Mvc;

namespace OT.Assessment.App.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PlayerController(IPublishEndpoint publishEndpoint,
                                  IPlayerService playerService,
                                  ITestComparisonService comparisonService,
                                  ILogger<PlayerController> logger) : ControllerBase
    {
        private static readonly ConcurrentBag<CasinoWager> _receivedAudit = [];
        private static readonly JsonSerializerOptions _serializerOptions = new() { WriteIndented = true };

        [HttpPost("casinowager")]
        public async Task<IActionResult> PostWager([FromBody] CasinoWager wager)
        {
            if (wager == null)
            {
                logger.LogWarning("Received null wager data");
                return BadRequest("Wager data is required");
            }

            // Capture the wager for the audit report
            _receivedAudit.Add(wager);

            await publishEndpoint.Publish(wager);

            return Ok();
        }

        [HttpGet("{playerId}/casino")] 
        public async Task<IActionResult> GetWagers(Guid playerId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var result = await playerService.GetPlayerWagersAsync(playerId, page, pageSize);
            return Ok(result);
        }

        [HttpGet("topSpenders")]
        public async Task<IActionResult> GetTopSpenders([FromQuery] int count = 10)
        {
            var result = await playerService.GetTopSpendersAsync(count);
            return Ok(result);
        }

        // DEBUG Endpoints - Not intended for production use
        [HttpGet("debug/clearData")]
        public async Task<IActionResult> ClearData()
        {
            _receivedAudit.Clear();
            await playerService.ClearAllDataAsync();

            return Ok(new { Message = "Data cleared successfully" });
        }

        [HttpGet("debug/testResults")]
        public async Task<IActionResult> Compare()
        {
            var dirPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "data_audit");
            var receivedFilePath = Path.Combine(dirPath, "received_wagers_audit.json");
            var json = JsonSerializer.Serialize(_receivedAudit, _serializerOptions);
            System.IO.File.WriteAllText(receivedFilePath, json);

            var report = await comparisonService.GenerateComparisonReport();
            return Content(report, "text/plain");
        }
    }
}