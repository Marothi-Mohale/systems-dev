namespace EVotingSystem.Models.Domain;

public class VoterAccount
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string PasswordSalt { get; set; } = string.Empty;
    public bool HasVoted { get; set; }
    public string? SelectedCandidateId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? LastLoginUtc { get; set; }
}
