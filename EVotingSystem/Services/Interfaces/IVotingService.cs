using EVotingSystem.Models.ViewModels;

namespace EVotingSystem.Services.Interfaces;

public interface IVotingService
{
    Task<BallotViewModel> GetBallotAsync(string voterName, CancellationToken cancellationToken);
}
