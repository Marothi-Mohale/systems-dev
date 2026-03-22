namespace EVotingSystem.Models.Domain;

public class ElectionDefinition
{
    public string Id { get; set; } = "default-election";
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime StartsAtUtc { get; set; } = DateTime.UtcNow.AddDays(-1);
    public DateTime EndsAtUtc { get; set; } = DateTime.UtcNow.AddDays(30);
    public int TotalPopulation { get; set; } = 100;
}
