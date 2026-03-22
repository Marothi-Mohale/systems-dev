using EVotingSystem.Options;
using Microsoft.Extensions.Options;

namespace EVotingSystem.Infrastructure.Firestore;

public class FirestoreCollectionNameProvider(IOptions<FirestoreOptions> options) : IFirestoreCollectionNameProvider
{
    private readonly FirestoreCollectionOptions collections = options.Value.Collections;

    public string Elections => collections.Elections;
    public string Candidates => collections.Candidates;
    public string Votes => collections.Votes;
    public string ElectionStats => collections.ElectionStats;
    public string VoterProfiles => collections.VoterProfiles;
}
