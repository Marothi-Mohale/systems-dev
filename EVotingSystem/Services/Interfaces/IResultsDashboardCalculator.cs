using EVotingSystem.Models.ViewModels;

namespace EVotingSystem.Services.Interfaces;

public interface IResultsDashboardCalculator
{
    PublicResultsViewModel BuildPresentationModel(PublicResultsViewModel source, int populationSize = 100);
    PublicResultsSnapshotViewModel BuildSnapshot(PublicResultsViewModel source);
}
