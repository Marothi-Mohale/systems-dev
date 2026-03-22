using EVotingSystem.Infrastructure.Firestore;
using EVotingSystem.Models.ViewModels;
using EVotingSystem.Services.Interfaces;

namespace EVotingSystem.Services;

public class ResultsService(
    IFirestoreElectionRepository repository,
    IResultsDashboardCalculator calculator,
    ILogger<ResultsService> logger) : IResultsService
{
    public async Task<PublicResultsViewModel> GetPublicDashboardAsync(CancellationToken cancellationToken)
    {
        try
        {
            await repository.EnsureSeedDataAsync(cancellationToken);
            var model = await repository.GetPublicDashboardAsync(cancellationToken);
            return calculator.BuildPresentationModel(model);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to build the public results dashboard.");
            return BuildUnavailableDashboard();
        }
    }

    public async Task<PublicResultsSnapshotViewModel> GetPublicSnapshotAsync(CancellationToken cancellationToken)
    {
        var dashboard = await GetPublicDashboardAsync(cancellationToken);
        return calculator.BuildSnapshot(dashboard);
    }

    private static PublicResultsViewModel BuildUnavailableDashboard()
    {
        return new PublicResultsViewModel
        {
            NoCandidatesState = true,
            ZeroVoteState = true,
            IsDataUnavailable = true,
            StatusNotice = "Live results are temporarily unavailable. Please refresh the page and try again shortly.",
            EmptyStateMessage = "Live results data could not be loaded safely at this time.",
            Statistics = new Models.Domain.PollStatistics
            {
                EligibleVoterCount = 100,
                ElectionOpen = false,
                GeneratedAtUtc = DateTime.UtcNow
            }
        };
    }
}
