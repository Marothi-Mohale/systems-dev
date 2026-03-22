using EVotingSystem.Options;
using Microsoft.Extensions.Options;

namespace EVotingSystem.Infrastructure.Firestore;

public class FirestoreSeedHostedService(
    IServiceProvider serviceProvider,
    IOptions<FirebaseOptions> firebaseOptions,
    IOptions<FirestoreOptions> firestoreOptions,
    ILogger<FirestoreSeedHostedService> logger) : IHostedService
{
    private readonly FirebaseOptions firebase = firebaseOptions.Value;
    private readonly FirestoreOptions firestore = firestoreOptions.Value;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!firestore.SeedOnStartup)
        {
            logger.LogInformation("Firestore startup seeding is disabled.");
            return;
        }

        if (!firebase.IsConfigured)
        {
            logger.LogInformation("Firestore startup seeding skipped because Firebase is not configured.");
            return;
        }

        try
        {
            using var scope = serviceProvider.CreateScope();
            var seedService = scope.ServiceProvider.GetRequiredService<IFirestoreSeedService>();
            await seedService.EnsureSeedDataAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Firestore seed initialization failed at startup.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
