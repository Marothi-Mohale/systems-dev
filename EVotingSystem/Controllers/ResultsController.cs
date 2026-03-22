using EVotingSystem.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace EVotingSystem.Controllers;

public class ResultsController(IResultsService resultsService, ILogger<ResultsController> logger) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        logger.LogInformation("Rendering public polling results dashboard.");
        var model = await resultsService.GetPublicDashboardAsync(cancellationToken);
        return View(model);
    }

    [HttpGet]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<IActionResult> Snapshot(CancellationToken cancellationToken)
    {
        var snapshot = await resultsService.GetPublicSnapshotAsync(cancellationToken);
        return Json(snapshot);
    }
}
