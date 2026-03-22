using EVotingSystem.Infrastructure.Firestore;
using EVotingSystem.Models.ViewModels;
using EVotingSystem.Services.Interfaces;

namespace EVotingSystem.Services;

public class ResultsService(
    IFirestoreElectionRepository repository,
    IResultsDashboardCalculator calculator) : IResultsService
{
    public async Task<PublicResultsViewModel> GetPublicDashboardAsync(CancellationToken cancellationToken)
    {
        await repository.EnsureSeedDataAsync(cancellationToken);
        var model = await repository.GetPublicDashboardAsync(cancellationToken);
        return calculator.BuildPresentationModel(model);
    }

    public async Task<PublicResultsSnapshotViewModel> GetPublicSnapshotAsync(CancellationToken cancellationToken)
    {
        var dashboard = await GetPublicDashboardAsync(cancellationToken);
        return calculator.BuildSnapshot(dashboard);
    }
}
