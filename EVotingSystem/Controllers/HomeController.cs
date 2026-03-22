using System.Diagnostics;
using EVotingSystem.Models;
using EVotingSystem.Services;
using Microsoft.AspNetCore.Mvc;

namespace EVotingSystem.Controllers;

public class HomeController(ElectionService electionService) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var model = await electionService.GetDashboardAsync(cancellationToken);
        return View(model);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
