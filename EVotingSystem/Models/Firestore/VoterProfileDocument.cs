using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace EVotingSystem.Models.Firestore;

public class VoterProfileDocument : FirestoreDocumentBase
{
    [JsonPropertyName("applicationUserId")]
    [Required]
    [StringLength(64)]
    public string ApplicationUserId { get; set; } = string.Empty;

    [JsonPropertyName("fullName")]
    [Required]
    [StringLength(120)]
    public string FullName { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("provinceCode")]
    [StringLength(16)]
    public string? ProvinceCode { get; set; }

    [JsonPropertyName("provinceName")]
    [StringLength(120)]
    public string? ProvinceName { get; set; }

    [JsonPropertyName("isEligibleToVote")]
    public bool IsEligibleToVote { get; set; } = true;

    [JsonPropertyName("hasVoted")]
    public bool HasVoted { get; set; }

    [JsonPropertyName("selectedCandidateId")]
    [StringLength(64)]
    public string? SelectedCandidateId { get; set; }

    [JsonPropertyName("registeredAtUtc")]
    public DateTime RegisteredAtUtc { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("lastLoginAtUtc")]
    public DateTime? LastLoginAtUtc { get; set; }

    [JsonPropertyName("lastVoteAtUtc")]
    public DateTime? LastVoteAtUtc { get; set; }
}
