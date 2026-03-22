namespace EVotingSystem.Infrastructure.Firestore;

public interface IFirestoreCollectionNameProvider
{
    string Elections { get; }
    string Candidates { get; }
    string Votes { get; }
    string ElectionStats { get; }
    string VoterProfiles { get; }
}
