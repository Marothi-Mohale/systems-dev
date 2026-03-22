using System.Diagnostics;
using EVotingSystem.Models;
using EVotingSystem.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace EVotingSystem.Controllers;

public class HomeController(IResultsService resultsService, ILogger<HomeController> logger) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        logger.LogInformation("Rendering public election dashboard.");
        var model = await resultsService.GetPublicDashboardAsync(cancellationToken);
        return View(model);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
