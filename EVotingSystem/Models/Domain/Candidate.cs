namespace EVotingSystem.Models.Domain;

public class Candidate
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Party { get; set; } = string.Empty;
    public string Slogan { get; set; } = string.Empty;
    public string Biography { get; set; } = string.Empty;
    public int VoteCount { get; set; }
}
