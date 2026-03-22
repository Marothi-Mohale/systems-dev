namespace EVotingSystem.Models.Domain;

public class PollResultRow
{
    public string CandidateId { get; set; } = string.Empty;
    public string CandidateName { get; set; } = string.Empty;
    public string Party { get; set; } = string.Empty;
    public int VoteCount { get; set; }
    public decimal VotePercentage { get; set; }
}
