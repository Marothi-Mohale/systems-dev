using System.ComponentModel.DataAnnotations;
using EVotingSystem.Models.Domain;

namespace EVotingSystem.Models.ViewModels;

public class VoteViewModel
{
    public ElectionDefinition Election { get; set; } = new();
    public IReadOnlyList<Candidate> Candidates { get; set; } = [];

    [Display(Name = "Candidate")]
    [Required(ErrorMessage = "Select a candidate before submitting your ballot.")]
    public string? SelectedCandidateId { get; set; }

    public bool AlreadyVoted { get; set; }

    [Required]
    [StringLength(120)]
    public string VoterName { get; set; } = string.Empty;

    [StringLength(256)]
    public string? ValidationMessage { get; set; }

    public VotingRules VotingRules { get; set; } = new();

    public bool CanSubmitVote => !AlreadyVoted && Election.IsVotingOpen(DateTime.UtcNow);
}
