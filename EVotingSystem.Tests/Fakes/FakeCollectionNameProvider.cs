using EVotingSystem.Infrastructure.Firestore;

namespace EVotingSystem.Tests.Fakes;

public sealed class FakeCollectionNameProvider : IFirestoreCollectionNameProvider
{
    public string Elections => "elections";
    public string Candidates => "candidates";
    public string Votes => "votes";
    public string ElectionStats => "electionStats";
    public string VoterProfiles => "voterProfiles";
}
