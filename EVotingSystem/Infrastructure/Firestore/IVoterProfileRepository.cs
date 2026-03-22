using EVotingSystem.Models.Domain;

namespace EVotingSystem.Infrastructure.Firestore;

public interface IVoterProfileRepository
{
    Task<VoterProfile?> GetByIdAsync(string voterId, CancellationToken cancellationToken);
    Task<bool> CreateIfMissingAsync(VoterProfile profile, CancellationToken cancellationToken);
}
