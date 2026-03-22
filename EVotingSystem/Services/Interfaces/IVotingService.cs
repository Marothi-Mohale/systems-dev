using EVotingSystem.Models.ViewModels;

namespace EVotingSystem.Services.Interfaces;

public interface IVotingService
{
    Task<VoteViewModel> GetBallotAsync(string voterName, CancellationToken cancellationToken);
}
