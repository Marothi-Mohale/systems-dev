using EVotingSystem.Models.ViewModels;

namespace EVotingSystem.Services.Interfaces;

public interface IResultsService
{
    Task<PublicDashboardViewModel> GetPublicDashboardAsync(CancellationToken cancellationToken);
}
