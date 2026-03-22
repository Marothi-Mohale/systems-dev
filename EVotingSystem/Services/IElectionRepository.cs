using EVotingSystem.Models.Domain;

namespace EVotingSystem.Services;

public interface IElectionRepository
{
    Task EnsureSeedDataAsync(CancellationToken cancellationToken);
    Task<ElectionDefinition> GetElectionAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<Candidate>> GetCandidatesAsync(CancellationToken cancellationToken);
    Task<VoterAccount?> GetVoterByEmailAsync(string email, CancellationToken cancellationToken);
    Task<VoterAccount?> GetVoterByIdAsync(string voterId, CancellationToken cancellationToken);
    Task<bool> CreateVoterAsync(VoterAccount voter, CancellationToken cancellationToken);
    Task<bool> UpdateLastLoginAsync(string voterId, DateTime lastLoginUtc, CancellationToken cancellationToken);
    Task<bool> CastVoteAsync(string voterId, string candidateId, CancellationToken cancellationToken);
}
