using EVotingSystem.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EVotingSystem.Controllers;

[AllowAnonymous]
public class ResultsController(IResultsService resultsService, ILogger<ResultsController> logger) : Controller
{
    [HttpGet]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        logger.LogDebug("Rendering public polling results dashboard.");
        var model = await resultsService.GetPublicDashboardAsync(cancellationToken);
        return View(model);
    }

    [HttpGet]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<IActionResult> Snapshot(CancellationToken cancellationToken)
    {
        try
        {
            var snapshot = await resultsService.GetPublicSnapshotAsync(cancellationToken);
            return Json(snapshot);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to produce the public results snapshot.");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                message = "Live results are temporarily unavailable."
            });
        }
    }
}
