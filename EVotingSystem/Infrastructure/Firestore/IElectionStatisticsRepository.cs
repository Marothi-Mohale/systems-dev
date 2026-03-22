using EVotingSystem.Models.Domain;

namespace EVotingSystem.Infrastructure.Firestore;

public interface IElectionStatisticsRepository
{
    Task<PollStatistics?> GetByElectionIdAsync(string electionId, CancellationToken cancellationToken);
    Task<bool> CreateIfMissingAsync(PollStatistics statistics, CancellationToken cancellationToken);
    Task UpsertAsync(PollStatistics statistics, CancellationToken cancellationToken);
}
