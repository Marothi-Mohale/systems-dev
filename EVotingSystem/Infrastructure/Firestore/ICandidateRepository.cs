using EVotingSystem.Models.Domain;

namespace EVotingSystem.Infrastructure.Firestore;

public interface ICandidateRepository
{
    Task<IReadOnlyList<Candidate>> GetActiveCandidatesAsync(CancellationToken cancellationToken);
    Task<Candidate?> GetByIdAsync(string candidateId, CancellationToken cancellationToken);
    Task<bool> CreateIfMissingAsync(Candidate candidate, CancellationToken cancellationToken);
}
