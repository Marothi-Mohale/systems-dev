using EVotingSystem.Models.ViewModels;
using EVotingSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EVotingSystem.Controllers;

[Authorize(Roles = "Voter")]
public class ElectionController(ElectionService electionService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Ballot(CancellationToken cancellationToken)
    {
        var model = await electionService.GetBallotAsync(cancellationToken);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Vote(BallotSubmissionViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var ballot = await electionService.GetBallotAsync(cancellationToken);
            ballot.ValidationMessage = "Please select a candidate before casting your vote.";
            return View("Ballot", ballot);
        }

        var result = await electionService.CastVoteAsync(model, cancellationToken);
        TempData["StatusMessage"] = result.Message;
        return RedirectToAction(nameof(Ballot));
    }
}
