namespace EVotingSystem.Services;

public class SeedHostedService(IServiceProvider serviceProvider, ILogger<SeedHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IElectionRepository>();

        try
        {
            await repository.EnsureSeedDataAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Seed data could not be initialized at startup.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
