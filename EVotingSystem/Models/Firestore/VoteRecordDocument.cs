using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace EVotingSystem.Models.Firestore;

public class VoteRecordDocument : FirestoreDocumentBase
{
    [JsonPropertyName("electionId")]
    [Required]
    [StringLength(64)]
    public string ElectionId { get; set; } = "default-election";

    [JsonPropertyName("voterId")]
    [Required]
    [StringLength(64)]
    public string VoterId { get; set; } = string.Empty;

    [JsonPropertyName("candidateId")]
    [Required]
    [StringLength(64)]
    public string CandidateId { get; set; } = string.Empty;

    [JsonPropertyName("votingChannel")]
    [Required]
    [StringLength(32)]
    public string VotingChannel { get; set; } = "web";

    [JsonPropertyName("status")]
    [Required]
    [StringLength(16)]
    public string Status { get; set; } = "accepted";

    [JsonPropertyName("rejectionReason")]
    [StringLength(256)]
    public string? RejectionReason { get; set; }

    [JsonPropertyName("idempotencyKey")]
    [StringLength(128)]
    public string? IdempotencyKey { get; set; }

    [JsonPropertyName("castAtUtc")]
    public DateTime CastAtUtc { get; set; } = DateTime.UtcNow;
}
