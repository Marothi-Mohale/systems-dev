using EVotingSystem.Models.ViewModels;
using EVotingSystem.Services.Interfaces;

namespace EVotingSystem.Services;

public class ResultsDashboardCalculator : IResultsDashboardCalculator
{
    public PublicResultsViewModel BuildPresentationModel(PublicResultsViewModel source, int populationSize = 100)
    {
        var candidateResults = NormalizeCandidateResults(source.CandidateResults);
        var totalVotes = candidateResults.Sum(result => result.VoteCount);
        var normalizedPopulation = populationSize <= 0 ? 100 : populationSize;
        var statistics = CloneStatistics(source, totalVotes, normalizedPopulation);
        var noCandidatesState = candidateResults.Count == 0;
        var zeroVoteState = totalVotes == 0;

        return new PublicResultsViewModel
        {
            Election = source.Election,
            Statistics = statistics,
            CandidateResults = candidateResults,
            AutoRefreshIntervalSeconds = source.AutoRefreshIntervalSeconds <= 0 ? 15 : source.AutoRefreshIntervalSeconds,
            ZeroVoteState = zeroVoteState,
            NoCandidatesState = noCandidatesState,
            EmptyStateMessage = GetEmptyStateMessage(noCandidatesState, zeroVoteState),
            PopulationSize = statistics.EligibleVoterCount
        };
    }

    public PublicResultsSnapshotViewModel BuildSnapshot(PublicResultsViewModel source)
    {
        return new PublicResultsSnapshotViewModel
        {
            TotalVotesCast = source.Statistics.TotalVotesCast,
            TurnoutPercentage = source.PopulationTurnoutPercentage,
            ElectionOpen = source.Statistics.ElectionOpen,
            GeneratedAtUtc = source.Statistics.GeneratedAtUtc,
            ZeroVoteState = source.ZeroVoteState,
            NoCandidatesState = source.NoCandidatesState,
            PopulationSize = source.PopulationSize,
            CandidateResults = source.CandidateResults
        };
    }

    private static List<CandidateResultViewModel> NormalizeCandidateResults(IEnumerable<CandidateResultViewModel> source)
    {
        var candidateResults = source
            .OrderByDescending(result => result.VoteCount)
            .ThenBy(result => result.CandidateName)
            .Select(result => new CandidateResultViewModel
            {
                CandidateId = result.CandidateId,
                CandidateName = result.CandidateName,
                Party = result.Party,
                VoteCount = result.VoteCount,
                VotePercentage = 0,
                IsLeading = false
            })
            .ToList();

        var totalVotes = candidateResults.Sum(result => result.VoteCount);
        var leadingVotes = candidateResults.Count == 0 ? 0 : candidateResults.Max(result => result.VoteCount);

        foreach (var result in candidateResults)
        {
            result.VotePercentage = CalculateVoteShare(result.VoteCount, totalVotes);
            result.IsLeading = leadingVotes > 0 && result.VoteCount == leadingVotes;
        }

        return candidateResults;
    }

    private static decimal CalculateVoteShare(int voteCount, int totalVotes)
    {
        return totalVotes <= 0
            ? 0
            : Math.Round((decimal)voteCount / totalVotes * 100m, 2, MidpointRounding.AwayFromZero);
    }

    private static Models.Domain.PollStatistics CloneStatistics(PublicResultsViewModel source, int totalVotes, int populationSize)
    {
        var statistics = source.Statistics ?? new Models.Domain.PollStatistics();
        return new Models.Domain.PollStatistics
        {
            ElectionId = string.IsNullOrWhiteSpace(statistics.ElectionId)
                ? source.Election.Id
                : statistics.ElectionId,
            TotalVotesCast = totalVotes,
            AcceptedVotes = totalVotes,
            RejectedVotes = statistics.RejectedVotes,
            EligibleVoterCount = populationSize,
            DistinctVoterCount = statistics.DistinctVoterCount == 0 ? totalVotes : statistics.DistinctVoterCount,
            ElectionOpen = statistics.ElectionOpen,
            GeneratedAtUtc = statistics.GeneratedAtUtc == default ? DateTime.UtcNow : statistics.GeneratedAtUtc,
            VotingRules = statistics.VotingRules
        };
    }

    private static string GetEmptyStateMessage(bool noCandidatesState, bool zeroVoteState)
    {
        if (noCandidatesState)
        {
            return "No candidates are currently available in the election database. Seed or publish candidate data to display public results.";
        }

        if (zeroVoteState)
        {
            return "No votes have been cast yet. Candidate percentages will update automatically after the first recorded ballot.";
        }

        return string.Empty;
    }
}
