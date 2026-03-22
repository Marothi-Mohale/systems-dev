namespace EVotingSystem.Options;

public class FirestoreOptions
{
    public const string SectionName = "Firestore";

    public bool SeedOnStartup { get; set; } = true;
    public FirestoreCollectionOptions Collections { get; set; } = new();
}

public class FirestoreCollectionOptions
{
    public string Elections { get; set; } = "elections";
    public string Candidates { get; set; } = "candidates";
    public string Votes { get; set; } = "votes";
    public string ElectionStats { get; set; } = "electionStats";
    public string VoterProfiles { get; set; } = "voterProfiles";
}
