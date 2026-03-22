namespace EVotingSystem.Models.Domain;

public class VoteRecord
{
    public string Id { get; set; } = string.Empty;
    public string VoterId { get; set; } = string.Empty;
    public string CandidateId { get; set; } = string.Empty;
    public DateTime CastAtUtc { get; set; }
}
