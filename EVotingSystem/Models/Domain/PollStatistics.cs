using System.ComponentModel.DataAnnotations;

namespace EVotingSystem.Models.Domain;

public class PollStatistics
{
    [Required]
    [StringLength(64, MinimumLength = 3)]
    [RegularExpression("^[a-z0-9]+(?:-[a-z0-9]+)*$")]
    public string ElectionId { get; set; } = "default-election";

    [Range(0, int.MaxValue)]
    public int TotalVotesCast { get; set; }

    [Range(0, int.MaxValue)]
    public int AcceptedVotes { get; set; }

    [Range(0, int.MaxValue)]
    public int RejectedVotes { get; set; }

    [Range(0, int.MaxValue)]
    public int EligibleVoterCount { get; set; }

    [Range(0, int.MaxValue)]
    public int DistinctVoterCount { get; set; }

    public bool ElectionOpen { get; set; }

    public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;

    public VotingRules VotingRules { get; set; } = new();

    public decimal TurnoutPercentage =>
        EligibleVoterCount <= 0
            ? 0
            : Math.Round((decimal)AcceptedVotes / EligibleVoterCount * 100m, 2);
}
