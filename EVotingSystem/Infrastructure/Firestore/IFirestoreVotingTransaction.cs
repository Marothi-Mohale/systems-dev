using EVotingSystem.Models.ViewModels;

namespace EVotingSystem.Infrastructure.Firestore;

public interface IFirestoreVotingTransaction
{
    Task<OperationResult> SubmitVoteAsync(string electionId, string voterId, string candidateId, CancellationToken cancellationToken);
}
