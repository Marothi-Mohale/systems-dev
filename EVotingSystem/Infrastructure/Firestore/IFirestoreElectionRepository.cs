using EVotingSystem.Models.ViewModels;

namespace EVotingSystem.Infrastructure.Firestore;

public interface IFirestoreElectionRepository
{
    Task EnsureSeedDataAsync(CancellationToken cancellationToken);
    Task<PublicResultsViewModel> GetPublicDashboardAsync(CancellationToken cancellationToken);
    Task<VoteViewModel> GetBallotAsync(string voterName, CancellationToken cancellationToken);
}
