using EVotingSystem.Models.ViewModels;

namespace EVotingSystem.Services.Interfaces;

public interface IVotingService
{
    Task<VoteViewModel> GetBallotAsync(string? voterId, string voterName, CancellationToken cancellationToken);
    Task<OperationResult> SubmitVoteAsync(string voterId, string candidateId, CancellationToken cancellationToken);
}
