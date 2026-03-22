using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace EVotingSystem.Models.Firestore;

public class PollStatisticsDocument : FirestoreDocumentBase
{
    [JsonPropertyName("electionId")]
    [Required]
    [StringLength(64)]
    public string ElectionId { get; set; } = "default-election";

    [JsonPropertyName("totalVotesCast")]
    [Range(0, int.MaxValue)]
    public int TotalVotesCast { get; set; }

    [JsonPropertyName("acceptedVotes")]
    [Range(0, int.MaxValue)]
    public int AcceptedVotes { get; set; }

    [JsonPropertyName("rejectedVotes")]
    [Range(0, int.MaxValue)]
    public int RejectedVotes { get; set; }

    [JsonPropertyName("eligibleVoterCount")]
    [Range(0, int.MaxValue)]
    public int EligibleVoterCount { get; set; }

    [JsonPropertyName("distinctVoterCount")]
    [Range(0, int.MaxValue)]
    public int DistinctVoterCount { get; set; }

    [JsonPropertyName("electionOpen")]
    public bool ElectionOpen { get; set; }

    [JsonPropertyName("generatedAtUtc")]
    public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("maxVotesPerVoterPerElection")]
    [Range(1, 1)]
    public int MaxVotesPerVoterPerElection { get; set; } = 1;

    [JsonPropertyName("requireAuthenticatedVoter")]
    public bool RequireAuthenticatedVoter { get; set; } = true;

    [JsonPropertyName("allowVoteReplacement")]
    public bool AllowVoteReplacement { get; set; }
}
