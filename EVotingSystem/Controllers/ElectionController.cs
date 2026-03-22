using EVotingSystem.Models.Identity;
using EVotingSystem.Models.ViewModels;
using EVotingSystem.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace EVotingSystem.Controllers;

[Authorize]
public class ElectionController(
    IVotingService votingService,
    UserManager<ApplicationUser> userManager,
    ILogger<ElectionController> logger) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Ballot(CancellationToken cancellationToken)
    {
        try
        {
            var user = await userManager.GetUserAsync(User);
            if (user is null || string.IsNullOrWhiteSpace(user.Id))
            {
                return Challenge();
            }

            var model = await votingService.GetBallotAsync(user.Id, user.FullName, cancellationToken);
            model.AlreadyVoted = model.AlreadyVoted || user.HasVoted;
            return View(model);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to load ballot for the authenticated user.");
            var fallback = new VoteViewModel
            {
                ValidationMessage = "The ballot is temporarily unavailable. Please refresh the page and try again."
            };

            return View(fallback);
        }
    }

    [HttpPost]
    [EnableRateLimiting("vote-submit")]
    public async Task<IActionResult> Ballot(VoteViewModel model, CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null || string.IsNullOrWhiteSpace(user.Id))
        {
            return Challenge();
        }

        model.SelectedCandidateId = model.SelectedCandidateId?.Trim();

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
            var duplicateModel = await votingService.GetBallotAsync(user.Id, user.FullName, cancellationToken);
            duplicateModel.SelectedCandidateId = model.SelectedCandidateId;
            duplicateModel.ValidationMessage = "You have already submitted your vote. Only one vote is allowed per voter.";
            duplicateModel.AlreadyVoted = true;
            return View(duplicateModel);
        }

        try
        {
            var result = await votingService.SubmitVoteAsync(user.Id, model.SelectedCandidateId!, cancellationToken);
            if (!result.Succeeded)
            {
                var failedModel = await votingService.GetBallotAsync(user.Id, user.FullName, cancellationToken);
                failedModel.SelectedCandidateId = model.SelectedCandidateId;
                failedModel.ValidationMessage = result.Message;
                failedModel.AlreadyVoted = failedModel.AlreadyVoted || user.HasVoted;
                return View(failedModel);
            }

            TempData["StatusMessage"] = result.Message;
            return RedirectToAction(nameof(Ballot));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unexpected ballot submission failure for user {UserId}.", user.Id);
            var failedModel = await votingService.GetBallotAsync(user.Id, user.FullName, cancellationToken);
            failedModel.SelectedCandidateId = model.SelectedCandidateId;
            failedModel.ValidationMessage = "We could not submit your ballot right now. Please try again.";
            failedModel.AlreadyVoted = failedModel.AlreadyVoted || user.HasVoted;
            return View(failedModel);
        }
    }
}
