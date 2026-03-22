using EVotingSystem.Models.Domain;

namespace EVotingSystem.Models.ViewModels;

public class BallotViewModel
{
    public ElectionDefinition Election { get; set; } = new();
    public IReadOnlyList<Candidate> Candidates { get; set; } = [];
    public bool AlreadyVoted { get; set; }
    public string? SelectedCandidateId { get; set; }
    public string VoterName { get; set; } = string.Empty;
    public string? ValidationMessage { get; set; }
}
