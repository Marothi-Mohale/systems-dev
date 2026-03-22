namespace EVotingSystem.Infrastructure.Firestore;

public interface IFirestoreSeedService
{
    Task EnsureSeedDataAsync(CancellationToken cancellationToken);
}
