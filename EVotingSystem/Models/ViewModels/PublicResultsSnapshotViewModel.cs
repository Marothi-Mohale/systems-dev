using System.ComponentModel.DataAnnotations;

namespace EVotingSystem.Models.ViewModels;

public class PublicResultsSnapshotViewModel
{
    [Range(0, int.MaxValue)]
    public int TotalVotesCast { get; set; }

    [Range(typeof(decimal), "0", "1000000")]
    public decimal TurnoutPercentage { get; set; }

    public bool ElectionOpen { get; set; }

    public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;

    public bool ZeroVoteState { get; set; }

    public bool NoCandidatesState { get; set; }

    [Range(1, int.MaxValue)]
    public int PopulationSize { get; set; } = 100;

    public IReadOnlyList<CandidateResultViewModel> CandidateResults { get; set; } = [];
}
