using System.ComponentModel.DataAnnotations;

namespace EVotingSystem.Models.ViewModels;

public class CandidateResultViewModel
{
    [Required]
    [StringLength(64)]
    public string CandidateId { get; set; } = string.Empty;

    [Required]
    [StringLength(120)]
    public string CandidateName { get; set; } = string.Empty;

    [Required]
    [StringLength(120)]
    public string Party { get; set; } = string.Empty;

    [Range(0, int.MaxValue)]
    public int VoteCount { get; set; }

    [Range(typeof(decimal), "0", "100")]
    public decimal VotePercentage { get; set; }

    public bool IsLeading { get; set; }
}
