using EVotingSystem.Infrastructure.Firestore;
using EVotingSystem.Models.ViewModels;
using EVotingSystem.Services.Interfaces;

namespace EVotingSystem.Services;

public class ResultsService(IFirestoreElectionRepository repository) : IResultsService
{
    public Task<PublicResultsViewModel> GetPublicDashboardAsync(CancellationToken cancellationToken) =>
        repository.GetPublicDashboardAsync(cancellationToken);
}
