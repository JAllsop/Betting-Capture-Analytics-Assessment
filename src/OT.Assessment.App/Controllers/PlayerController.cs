using System.Collections.Concurrent;
using System.Text.Json;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using OT.Assessment.Shared;

namespace OT.Assessment.App.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PlayerController(IPublishEndpoint publishEndpoint, ILogger<PlayerController> logger) : ControllerBase
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

        [HttpPost("debug/save-audit")]
        public IActionResult SaveAudit()
        {
            var dirPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "data_audit");
            var receivedFilePath = Path.Combine(dirPath, "received_wagers_audit.json");
            var json = JsonSerializer.Serialize(_receivedAudit, _serializerOptions);
            System.IO.File.WriteAllText(receivedFilePath, json);

            return Ok(new
            {
                Message = "Audit saved successfully",
                TotalReceived = _receivedAudit.Count,
                Path = receivedFilePath
            });
        }

        [HttpGet("{playerId}/wagers")]
        public IActionResult GetWagers(Guid playerId) => Ok();

        [HttpGet("topSpenders")]
        public IActionResult GetTopSpenders([FromQuery] int count = 10) => Ok();
    }
}