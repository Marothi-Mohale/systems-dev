using EVotingSystem.Models.ViewModels;

namespace EVotingSystem.Services.Interfaces;

public interface IResultsService
{
    Task<PublicResultsViewModel> GetPublicDashboardAsync(CancellationToken cancellationToken);
    Task<PublicResultsSnapshotViewModel> GetPublicSnapshotAsync(CancellationToken cancellationToken);
}
