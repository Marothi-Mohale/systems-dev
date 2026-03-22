using EVotingSystem.Infrastructure.Firestore;
using EVotingSystem.Models.ViewModels;
using EVotingSystem.Services.Interfaces;

namespace EVotingSystem.Services;

public class VotingService(IFirestoreElectionRepository repository) : IVotingService
{
    public Task<VoteViewModel> GetBallotAsync(string voterName, CancellationToken cancellationToken) =>
        repository.GetBallotAsync(voterName, cancellationToken);
}
