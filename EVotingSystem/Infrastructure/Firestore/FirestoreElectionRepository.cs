using EVotingSystem.Models.Domain;
using EVotingSystem.Models.ViewModels;
using EVotingSystem.Options;
using Microsoft.Extensions.Options;

namespace EVotingSystem.Infrastructure.Firestore;

public class FirestoreElectionRepository(IOptions<SeedOptions> seedOptions) : IFirestoreElectionRepository
{
    private readonly SeedOptions seeds = seedOptions.Value;

    public Task<BallotViewModel> GetBallotAsync(string voterName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // TODO: Replace seeded data with Firestore reads from the `candidates`
        // and `elections` collections once the Firebase integration is wired in.
        return Task.FromResult(new BallotViewModel
        {
            Election = seeds.Election,
            Candidates = seeds.Candidates,
            VoterName = string.IsNullOrWhiteSpace(voterName) ? "Registered voter" : voterName
        });
    }

    public Task<PublicDashboardViewModel> GetPublicDashboardAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var totalVotes = seeds.Candidates.Sum(candidate => candidate.VoteCount);
        var resultRows = seeds.Candidates
            .OrderBy(candidate => candidate.Name)
            .Select(candidate => new PollResultRow
            {
                CandidateId = candidate.Id,
                CandidateName = candidate.Name,
                Party = candidate.Party,
                VoteCount = candidate.VoteCount,
                VotePercentage = totalVotes == 0
                    ? 0
                    : Math.Round((decimal)candidate.VoteCount / totalVotes * 100, 2)
            })
            .ToList();

        return Task.FromResult(new PublicDashboardViewModel
        {
            Election = seeds.Election,
            Results = resultRows,
            TotalVotes = totalVotes,
            PopulationTurnoutPercentage = seeds.Election.TotalPopulation == 0
                ? 0
                : Math.Round((decimal)totalVotes / seeds.Election.TotalPopulation * 100, 2),
            ElectionOpen = DateTime.UtcNow >= seeds.Election.StartsAtUtc && DateTime.UtcNow <= seeds.Election.EndsAtUtc,
            GeneratedAtUtc = DateTime.UtcNow
        });
    }
}
