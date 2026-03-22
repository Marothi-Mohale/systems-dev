using EVotingSystem.Infrastructure.Firestore;
using EVotingSystem.Models.ViewModels;
using EVotingSystem.Services.Interfaces;

namespace EVotingSystem.Services;

public class VotingService(IFirestoreElectionRepository repository) : IVotingService
{
    public async Task<VoteViewModel> GetBallotAsync(string voterName, CancellationToken cancellationToken)
    {
        await repository.EnsureSeedDataAsync(cancellationToken);
        return await repository.GetBallotAsync(voterName, cancellationToken);
    }
}
