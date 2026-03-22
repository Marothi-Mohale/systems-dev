using System.ComponentModel.DataAnnotations;

namespace EVotingSystem.Models.Domain;

public class ElectionDefinition
{
    [Required]
    [StringLength(64, MinimumLength = 3)]
    [RegularExpression("^[a-z0-9]+(?:-[a-z0-9]+)*$")]
    public string Id { get; set; } = "default-election";

    [Required]
    [StringLength(160, MinimumLength = 3)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(2000, MinimumLength = 10)]
    public string Description { get; set; } = string.Empty;

    public DateTime StartsAtUtc { get; set; } = DateTime.UtcNow.AddDays(-1);
    public DateTime EndsAtUtc { get; set; } = DateTime.UtcNow.AddDays(30);

    [Range(1, int.MaxValue)]
    public int TotalPopulation { get; set; } = 100;

    public VotingRules VotingRules { get; set; } = new();

    public bool IsVotingOpen(DateTime utcNow) => utcNow >= StartsAtUtc && utcNow <= EndsAtUtc;
}
