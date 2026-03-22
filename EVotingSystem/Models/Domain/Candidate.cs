using System.ComponentModel.DataAnnotations;

namespace EVotingSystem.Models.Domain;

public class Candidate
{
    [Required]
    [StringLength(64, MinimumLength = 3)]
    [RegularExpression("^[a-z0-9]+(?:-[a-z0-9]+)*$")]
    public string Id { get; set; } = string.Empty;

    [Required]
    [StringLength(64, MinimumLength = 3)]
    [RegularExpression("^[a-z0-9]+(?:-[a-z0-9]+)*$")]
    public string ElectionId { get; set; } = "default-election";

    [Required]
    [StringLength(120, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(120, MinimumLength = 2)]
    public string Party { get; set; } = string.Empty;

    [StringLength(160)]
    public string Slogan { get; set; } = string.Empty;

    [StringLength(2000)]
    public string Biography { get; set; } = string.Empty;

    [Url]
    [StringLength(2048)]
    public string? PhotoUrl { get; set; }

    [Range(0, int.MaxValue)]
    public int VoteCount { get; set; }

    public bool IsActive { get; set; } = true;

    [Range(0, 10_000)]
    public int DisplayOrder { get; set; }

    [StringLength(16)]
    public string? ProvinceCode { get; set; }

    [StringLength(120)]
    public string? ProvinceName { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
