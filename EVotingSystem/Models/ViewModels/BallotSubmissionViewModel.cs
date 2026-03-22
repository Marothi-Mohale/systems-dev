using System.ComponentModel.DataAnnotations;

namespace EVotingSystem.Models.ViewModels;

public class BallotSubmissionViewModel
{
    [Required]
    public string CandidateId { get; set; } = string.Empty;
}
