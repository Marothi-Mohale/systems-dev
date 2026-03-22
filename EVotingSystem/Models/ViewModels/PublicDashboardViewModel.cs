using EVotingSystem.Models.Domain;

namespace EVotingSystem.Models.ViewModels;

public class PublicDashboardViewModel
{
    public ElectionDefinition Election { get; set; } = new();
    public IReadOnlyList<PollResultRow> Results { get; set; } = [];
    public int TotalVotes { get; set; }
    public decimal PopulationTurnoutPercentage { get; set; }
    public bool ElectionOpen { get; set; }
    public DateTime GeneratedAtUtc { get; set; }
}
