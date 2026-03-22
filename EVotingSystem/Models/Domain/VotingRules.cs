using System.ComponentModel.DataAnnotations;

namespace EVotingSystem.Models.Domain;

public class VotingRules
{
    [Range(1, 1)]
    public int MaxVotesPerVoterPerElection { get; set; } = 1;

    public bool RequireAuthenticatedVoter { get; set; } = true;
    public bool RequireEligibleVoter { get; set; } = true;
    public bool AllowVoteReplacement { get; set; }
    public bool AllowVoteRevocation { get; set; }
    public bool EnforceElectionWindow { get; set; } = true;
}
