using EVotingSystem.Infrastructure.Firestore;
using EVotingSystem.Models.ViewModels;
using EVotingSystem.Services.Interfaces;

namespace EVotingSystem.Services;

public class ResultsService(IFirestoreElectionRepository repository) : IResultsService
{
    public async Task<PublicResultsViewModel> GetPublicDashboardAsync(CancellationToken cancellationToken)
    {
        await repository.EnsureSeedDataAsync(cancellationToken);
        return await repository.GetPublicDashboardAsync(cancellationToken);
    }
}
