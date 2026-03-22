using EVotingSystem.Models.Domain;
using EVotingSystem.Models.ViewModels;
using EVotingSystem.Options;
using Microsoft.Extensions.Options;

namespace EVotingSystem.Infrastructure.Firestore;

public class FirestoreElectionRepository(
    ICandidateRepository candidateRepository,
    IVoteRepository voteRepository,
    IElectionStatisticsRepository statisticsRepository,
    IFirestoreSeedService seedService,
    IOptions<FirebaseOptions> firebaseOptions,
    IOptions<SeedOptions> seedOptions,
    ILogger<FirestoreElectionRepository> logger) : IFirestoreElectionRepository
{
    private readonly FirebaseOptions firebase = firebaseOptions.Value;
    private readonly SeedOptions seeds = seedOptions.Value;

    public Task EnsureSeedDataAsync(CancellationToken cancellationToken) =>
        seedService.EnsureSeedDataAsync(cancellationToken);

    public async Task<VoteViewModel> GetBallotAsync(string voterName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var candidates = firebase.IsConfigured
            ? await candidateRepository.GetActiveCandidatesAsync(cancellationToken)
            : seeds.Candidates
                .Where(candidate => candidate.IsActive)
                .OrderBy(candidate => candidate.DisplayOrder)
                .ThenBy(candidate => candidate.Name)
                .ToList();

        if (!firebase.IsConfigured)
        {
            logger.LogInformation("Using seeded candidate data because Firebase is not configured.");
        }

        return new VoteViewModel
        {
            Election = seeds.Election,
            Candidates = candidates,
            VoterName = string.IsNullOrWhiteSpace(voterName) ? "Registered voter" : voterName,
            VotingRules = seeds.Election.VotingRules
        };
    }

    public async Task<PublicResultsViewModel> GetPublicDashboardAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var candidates = firebase.IsConfigured
            ? await candidateRepository.GetActiveCandidatesAsync(cancellationToken)
            : seeds.Candidates
                .Where(candidate => candidate.IsActive)
                .OrderBy(candidate => candidate.DisplayOrder)
                .ThenBy(candidate => candidate.Name)
                .ToList();

        var totalVotes = candidates.Sum(candidate => candidate.VoteCount);
        var resultRows = candidates
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

        var statistics = firebase.IsConfigured
            ? await statisticsRepository.GetByElectionIdAsync(seeds.Election.Id, cancellationToken)
            : null;

        if (statistics is null)
        {
            statistics = new PollStatistics
            {
                ElectionId = seeds.Election.Id,
                TotalVotesCast = totalVotes,
                AcceptedVotes = totalVotes,
                EligibleVoterCount = seeds.Election.TotalPopulation,
                DistinctVoterCount = firebase.IsConfigured
                    ? await voteRepository.CountAcceptedVotesAsync(seeds.Election.Id, cancellationToken)
                    : totalVotes,
                ElectionOpen = seeds.Election.IsVotingOpen(DateTime.UtcNow),
                GeneratedAtUtc = DateTime.UtcNow,
                VotingRules = seeds.Election.VotingRules
            };
        }
        else
        {
            statistics.TotalVotesCast = totalVotes;
            statistics.AcceptedVotes = totalVotes;
            statistics.DistinctVoterCount = statistics.DistinctVoterCount == 0
                ? totalVotes
                : statistics.DistinctVoterCount;
            statistics.EligibleVoterCount = statistics.EligibleVoterCount == 0
                ? seeds.Election.TotalPopulation
                : statistics.EligibleVoterCount;
            statistics.ElectionOpen = seeds.Election.IsVotingOpen(DateTime.UtcNow);
            statistics.GeneratedAtUtc = DateTime.UtcNow;
        }

        return new PublicResultsViewModel
        {
            Election = seeds.Election,
            CandidateResults = resultRows,
            Statistics = statistics
        };
    }
}
