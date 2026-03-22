using EVotingSystem.Models.Domain;
using EVotingSystem.Models.ViewModels;
using EVotingSystem.Services;
using EVotingSystem.Tests.Testing;

namespace EVotingSystem.Tests.Services;

public static class ResultsDashboardCalculatorTests
{
    public static IEnumerable<TestCase> GetCases()
    {
        yield return new TestCase(
            "Results dashboard calculates vote percentages correctly",
            CalculatesPercentagesCorrectlyAsync);
        yield return new TestCase(
            "Results dashboard handles zero vote state safely",
            HandlesZeroVoteStateAsync);
    }

    private static Task CalculatesPercentagesCorrectlyAsync()
    {
        var calculator = new ResultsDashboardCalculator();
        var source = new PublicResultsViewModel
        {
            CandidateResults =
            [
                new CandidateResultViewModel { CandidateId = "a", CandidateName = "Candidate A", Party = "Party A", VoteCount = 2 },
                new CandidateResultViewModel { CandidateId = "b", CandidateName = "Candidate B", Party = "Party B", VoteCount = 1 }
            ],
            Statistics = new PollStatistics
            {
                ElectionId = "2026-national-election",
                ElectionOpen = true
            }
        };

        var result = calculator.BuildPresentationModel(source, populationSize: 100);

        AssertEx.Equal(3, result.Statistics.TotalVotesCast);
        AssertEx.Equal(3m, result.PopulationTurnoutPercentage);
        AssertEx.Equal(66.67m, result.CandidateResults[0].VotePercentage);
        AssertEx.Equal(33.33m, result.CandidateResults[1].VotePercentage);
        AssertEx.True(result.CandidateResults[0].IsLeading);
        AssertEx.False(result.ZeroVoteState);
        return Task.CompletedTask;
    }

    private static Task HandlesZeroVoteStateAsync()
    {
        var calculator = new ResultsDashboardCalculator();
        var source = new PublicResultsViewModel
        {
            CandidateResults =
            [
                new CandidateResultViewModel { CandidateId = "a", CandidateName = "Candidate A", Party = "Party A", VoteCount = 0 }
            ],
            Statistics = new PollStatistics
            {
                ElectionId = "2026-national-election"
            }
        };

        var result = calculator.BuildPresentationModel(source, populationSize: 100);

        AssertEx.True(result.ZeroVoteState);
        AssertEx.Equal(0, result.Statistics.TotalVotesCast);
        AssertEx.Equal(0m, result.CandidateResults[0].VotePercentage);
        AssertEx.Equal(0m, result.PopulationTurnoutPercentage);
        return Task.CompletedTask;
    }
}
