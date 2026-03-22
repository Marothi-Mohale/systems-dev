using System.ComponentModel.DataAnnotations;

namespace EVotingSystem.Models.Domain;

public class VoterProfile
{
    [Required]
    [StringLength(64, MinimumLength = 3)]
    public string Id { get; set; } = string.Empty;

    [Required]
    [StringLength(64, MinimumLength = 3)]
    public string ApplicationUserId { get; set; } = string.Empty;

    [Required]
    [StringLength(120, MinimumLength = 2)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [StringLength(16)]
    public string? ProvinceCode { get; set; }

    [StringLength(120)]
    public string? ProvinceName { get; set; }

    public bool IsEligibleToVote { get; set; } = true;
    public bool HasVoted { get; set; }

    [StringLength(64)]
    public string? SelectedCandidateId { get; set; }

    public DateTime RegisteredAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAtUtc { get; set; }
    public DateTime? LastVoteAtUtc { get; set; }
}
