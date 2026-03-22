using EVotingSystem.Infrastructure.Firestore;
using EVotingSystem.Models.Identity;
using EVotingSystem.Models.ViewModels;
using EVotingSystem.Services.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace EVotingSystem.Services;

public class VotingService(
    IFirestoreElectionRepository repository,
    UserManager<ApplicationUser> userManager,
    ILogger<VotingService> logger) : IVotingService
{
    public async Task<VoteViewModel> GetBallotAsync(string? voterId, string voterName, CancellationToken cancellationToken)
    {
        await repository.EnsureSeedDataAsync(cancellationToken);
        return await repository.GetBallotAsync(voterId, voterName, cancellationToken);
    }

    public async Task<OperationResult> SubmitVoteAsync(string voterId, string candidateId, CancellationToken cancellationToken)
    {
        await repository.EnsureSeedDataAsync(cancellationToken);
        var user = await userManager.FindByIdAsync(voterId);
        if (user is not null && user.HasVoted)
        {
            logger.LogInformation("Vote submission blocked early because user {UserId} is already marked as voted.", voterId);
            return OperationResult.Failure("You have already submitted your vote. Only one vote is allowed per voter.");
        }

        var result = await repository.SubmitVoteAsync(voterId, candidateId, cancellationToken);
        if (!result.Succeeded)
        {
            logger.LogWarning("Vote submission failed for user {UserId} and candidate {CandidateId}: {Message}", voterId, candidateId, result.Message);
            return result;
        }

        if (user is not null)
        {
            user.HasVoted = true;
            user.ActiveElectionId ??= "2026-national-election";
            var identityResult = await userManager.UpdateAsync(user);
            if (!identityResult.Succeeded)
            {
                logger.LogWarning("Vote succeeded for user {UserId}, but updating Identity vote state failed.", voterId);
            }
        }

        logger.LogInformation("Vote submission succeeded for user {UserId} and candidate {CandidateId}.", voterId, candidateId);
        return result;
    }
}
