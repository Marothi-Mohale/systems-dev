using EVotingSystem.Models.ViewModels;

namespace EVotingSystem.Infrastructure.Firestore;

public interface IFirestoreElectionRepository
{
    Task<PublicDashboardViewModel> GetPublicDashboardAsync(CancellationToken cancellationToken);
    Task<BallotViewModel> GetBallotAsync(string voterName, CancellationToken cancellationToken);
}
