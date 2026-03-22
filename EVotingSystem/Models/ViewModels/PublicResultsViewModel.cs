using EVotingSystem.Models.Domain;
using System.ComponentModel.DataAnnotations;

namespace EVotingSystem.Models.ViewModels;

public class PublicResultsViewModel
{
    public ElectionDefinition Election { get; set; } = new();
    public PollStatistics Statistics { get; set; } = new();
    public IReadOnlyList<CandidateResultViewModel> CandidateResults { get; set; } = [];

    [Range(1, int.MaxValue)]
    public int PopulationSize { get; set; } = 100;

    public bool ZeroVoteState { get; set; }
    public bool NoCandidatesState { get; set; }

    [StringLength(240)]
    public string EmptyStateMessage { get; set; } = string.Empty;

    [Range(1, 300)]
    public int AutoRefreshIntervalSeconds { get; set; } = 15;

    public decimal PopulationTurnoutPercentage => Statistics.TurnoutPercentage;
}
