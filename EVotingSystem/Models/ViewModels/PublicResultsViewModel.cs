using EVotingSystem.Models.Domain;

namespace EVotingSystem.Models.ViewModels;

public class PublicResultsViewModel
{
    public ElectionDefinition Election { get; set; } = new();
    public PollStatistics Statistics { get; set; } = new();
    public IReadOnlyList<CandidateResultViewModel> CandidateResults { get; set; } = [];
}
