using System.ComponentModel.DataAnnotations;
using EVotingSystem.Models.Domain;

namespace EVotingSystem.Models.ViewModels;

public class VoteViewModel
{
    public ElectionDefinition Election { get; set; } = new();
    public IReadOnlyList<Candidate> Candidates { get; set; } = [];

    [Display(Name = "Candidate")]
    [Required(ErrorMessage = "Select a candidate before submitting your ballot.")]
    [StringLength(64)]
    public string? SelectedCandidateId { get; set; }

    public bool AlreadyVoted { get; set; }

    [StringLength(120)]
    public string VoterName { get; set; } = string.Empty;

    [StringLength(256)]
    public string? ValidationMessage { get; set; }

    public bool HasCandidates => Candidates.Count > 0;

    public VotingRules VotingRules { get; set; } = new();

    public bool CanSubmitVote => HasCandidates && !AlreadyVoted && Election.IsVotingOpen(DateTime.UtcNow);
}
