using EVotingSystem.Models.Domain;

namespace EVotingSystem.Options;

public class SeedOptions
{
    public const string SectionName = "Seed";

    public ElectionDefinition Election { get; set; } = new();
    public List<Candidate> Candidates { get; set; } = [];
}
