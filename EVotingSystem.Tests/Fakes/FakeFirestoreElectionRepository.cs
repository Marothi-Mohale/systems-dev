using EVotingSystem.Infrastructure.Firestore;
using EVotingSystem.Models.ViewModels;

namespace EVotingSystem.Tests.Fakes;

public sealed class FakeFirestoreElectionRepository : IFirestoreElectionRepository
{
    public int EnsureSeedCallCount { get; private set; }
    public int SubmitVoteCallCount { get; private set; }
    public Func<string, string, CancellationToken, Task<OperationResult>>? OnSubmitVoteAsync { get; set; }
    public Func<string?, string, CancellationToken, Task<VoteViewModel>>? OnGetBallotAsync { get; set; }
    public Func<CancellationToken, Task<PublicResultsViewModel>>? OnGetPublicDashboardAsync { get; set; }

    public Task EnsureSeedDataAsync(CancellationToken cancellationToken)
    {
        EnsureSeedCallCount++;
        return Task.CompletedTask;
    }

    public Task<PublicResultsViewModel> GetPublicDashboardAsync(CancellationToken cancellationToken) =>
        OnGetPublicDashboardAsync?.Invoke(cancellationToken)
        ?? Task.FromResult(new PublicResultsViewModel());

    public Task<VoteViewModel> GetBallotAsync(string? voterId, string voterName, CancellationToken cancellationToken) =>
        OnGetBallotAsync?.Invoke(voterId, voterName, cancellationToken)
        ?? Task.FromResult(new VoteViewModel { VoterName = voterName });

    public Task<OperationResult> SubmitVoteAsync(string voterId, string candidateId, CancellationToken cancellationToken)
    {
        SubmitVoteCallCount++;
        return OnSubmitVoteAsync?.Invoke(voterId, candidateId, cancellationToken)
            ?? Task.FromResult(OperationResult.Success("Vote recorded."));
    }
}
