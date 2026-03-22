using EVotingSystem.Models.Domain;

namespace EVotingSystem.Infrastructure.Firestore;

public interface IVoteRepository
{
    Task<bool> ExistsForVoterAsync(string electionId, string voterId, CancellationToken cancellationToken);
    Task CreateAsync(VoteRecord vote, CancellationToken cancellationToken);
    Task<int> CountAcceptedVotesAsync(string electionId, CancellationToken cancellationToken);
}
