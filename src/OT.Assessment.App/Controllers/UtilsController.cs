using Microsoft.AspNetCore.Mvc;
using OT.Assessment.App.Services;

namespace OT.Assessment.App.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UtilsController(ITestComparisonService comparisonService) : ControllerBase
    {
        [HttpGet("clearDB")]
        public ActionResult Index()
        {
            return Ok("This endpoint is a placeholder for potential future utilities. For now, it does nothing.");
        }


        [HttpGet("compare")]
        public async Task<IActionResult> Compare()
        {
            var report = await comparisonService.GenerateComparisonReport();
            return Content(report, "text/plain");
        }
    }
}
