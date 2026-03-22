using EVotingSystem.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EVotingSystem.Controllers;

[Authorize]
public class ElectionController(IVotingService votingService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Ballot(CancellationToken cancellationToken)
    {
        var voterName = User.Identity?.Name ?? "Registered voter";
        var model = await votingService.GetBallotAsync(voterName, cancellationToken);
        return View(model);
    }
}
