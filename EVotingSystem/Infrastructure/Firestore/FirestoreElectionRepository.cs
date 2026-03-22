using EVotingSystem.Models.Domain;
using EVotingSystem.Models.ViewModels;
using EVotingSystem.Options;
using Microsoft.Extensions.Options;

namespace EVotingSystem.Infrastructure.Firestore;

public class FirestoreElectionRepository(IOptions<SeedOptions> seedOptions) : IFirestoreElectionRepository
{
    private readonly SeedOptions seeds = seedOptions.Value;

    public Task<VoteViewModel> GetBallotAsync(string voterName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // TODO: Replace seeded data with Firestore reads from the `candidates`
        // and `elections` collections once the Firebase integration is wired in.
        return Task.FromResult(new VoteViewModel
        {
            Election = seeds.Election,
            Candidates = seeds.Candidates,
            VoterName = string.IsNullOrWhiteSpace(voterName) ? "Registered voter" : voterName,
            VotingRules = seeds.Election.VotingRules
        });
    }

    public Task<PublicResultsViewModel> GetPublicDashboardAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var totalVotes = seeds.Candidates.Sum(candidate => candidate.VoteCount);
        var resultRows = seeds.Candidates
            .OrderByDescending(candidate => candidate.VoteCount)
            .ThenBy(candidate => candidate.Name)
            .Select(candidate => new CandidateResultViewModel
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

        var maxVotes = resultRows.Count == 0 ? 0 : resultRows.Max(row => row.VoteCount);
        foreach (var row in resultRows)
        {
            row.IsLeading = row.VoteCount == maxVotes && maxVotes > 0;
        }

        return Task.FromResult(new PublicResultsViewModel
        {
            Election = seeds.Election,
            CandidateResults = resultRows,
            Statistics = new PollStatistics
            {
                ElectionId = seeds.Election.Id,
                TotalVotesCast = totalVotes,
                AcceptedVotes = totalVotes,
                EligibleVoterCount = seeds.Election.TotalPopulation,
                DistinctVoterCount = totalVotes,
                ElectionOpen = seeds.Election.IsVotingOpen(DateTime.UtcNow),
                GeneratedAtUtc = DateTime.UtcNow,
                VotingRules = seeds.Election.VotingRules
            }
        });
    }
}
