using EVotingSystem.Models.Identity;
using EVotingSystem.Models.ViewModels;
using EVotingSystem.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace EVotingSystem.Controllers;

[Authorize]
public class ElectionController(
    IVotingService votingService,
    UserManager<ApplicationUser> userManager) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Ballot(CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        var model = await votingService.GetBallotAsync(user.Id, user.FullName, cancellationToken);
        model.AlreadyVoted = model.AlreadyVoted || user.HasVoted;
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Ballot(VoteViewModel model, CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        if (!ModelState.IsValid)
        {
            var invalidModel = await votingService.GetBallotAsync(user.Id, user.FullName, cancellationToken);
            invalidModel.SelectedCandidateId = model.SelectedCandidateId;
            invalidModel.ValidationMessage = "Select one candidate before submitting your ballot.";
            invalidModel.AlreadyVoted = invalidModel.AlreadyVoted || user.HasVoted;
            return View(invalidModel);
        }

        if (user.HasVoted)
        {
            var existingVoteModel = await votingService.GetBallotAsync(user.Id, user.FullName, cancellationToken);
            existingVoteModel.AlreadyVoted = true;
            existingVoteModel.ValidationMessage = "You have already submitted your vote. Only one vote is allowed per voter.";
            return View(existingVoteModel);
        }

        var result = await votingService.SubmitVoteAsync(user.Id, model.SelectedCandidateId!, cancellationToken);
        if (!result.Succeeded)
        {
            var failedModel = await votingService.GetBallotAsync(user.Id, user.FullName, cancellationToken);
            failedModel.SelectedCandidateId = model.SelectedCandidateId;
            failedModel.ValidationMessage = result.Message;
            failedModel.AlreadyVoted = failedModel.AlreadyVoted || user.HasVoted;
            return View(failedModel);
        }

        user.HasVoted = true;
        user.ActiveElectionId = "2026-national-election";
        await userManager.UpdateAsync(user);

        TempData["StatusMessage"] = result.Message;
        return RedirectToAction(nameof(Ballot));
    }
}
