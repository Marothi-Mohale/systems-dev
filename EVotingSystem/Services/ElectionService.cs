using EVotingSystem.Models.Domain;
using EVotingSystem.Models.ViewModels;

namespace EVotingSystem.Services;

public class ElectionService(
    IElectionRepository repository,
    CurrentUserService currentUserService,
    PasswordHasher passwordHasher,
    EmailVerificationService emailVerificationService)
{
    public async Task<PublicDashboardViewModel> GetDashboardAsync(CancellationToken cancellationToken)
    {
        await repository.EnsureSeedDataAsync(cancellationToken);

        var election = await repository.GetElectionAsync(cancellationToken);
        var candidates = await repository.GetCandidatesAsync(cancellationToken);
        var totalVotes = candidates.Sum(candidate => candidate.VoteCount);

        var results = candidates
            .Select(candidate => new PollResultRow
            {
                CandidateId = candidate.Id,
                CandidateName = candidate.Name,
                Party = candidate.Party,
                VoteCount = candidate.VoteCount,
                VotePercentage = totalVotes == 0 ? 0 : Math.Round((decimal)candidate.VoteCount / totalVotes * 100, 2)
            })
            .OrderByDescending(candidate => candidate.VoteCount)
            .ThenBy(candidate => candidate.CandidateName)
            .ToList();

        return new PublicDashboardViewModel
        {
            Election = election,
            Results = results,
            TotalVotes = totalVotes,
            PopulationTurnoutPercentage = election.TotalPopulation == 0 ? 0 : Math.Round((decimal)totalVotes / election.TotalPopulation * 100, 2),
            ElectionOpen = DateTime.UtcNow >= election.StartsAtUtc && DateTime.UtcNow <= election.EndsAtUtc,
            GeneratedAtUtc = DateTime.UtcNow
        };
    }

    public async Task<BallotViewModel> GetBallotAsync(CancellationToken cancellationToken)
    {
        await repository.EnsureSeedDataAsync(cancellationToken);

        var election = await repository.GetElectionAsync(cancellationToken);
        var candidates = await repository.GetCandidatesAsync(cancellationToken);
        var voterId = currentUserService.UserId;
        var voter = string.IsNullOrWhiteSpace(voterId) ? null : await repository.GetVoterByIdAsync(voterId, cancellationToken);

        return new BallotViewModel
        {
            Election = election,
            Candidates = candidates,
            AlreadyVoted = voter?.HasVoted == true,
            SelectedCandidateId = voter?.SelectedCandidateId,
            VoterName = currentUserService.FullName ?? voter?.FullName ?? "Voter"
        };
    }

    public async Task<OperationResult> RegisterVoterAsync(RegisterViewModel model, CancellationToken cancellationToken)
    {
        await repository.EnsureSeedDataAsync(cancellationToken);

        var normalizedEmail = model.Email.Trim().ToLowerInvariant();
        var emailVerification = await emailVerificationService.VerifyAsync(normalizedEmail, cancellationToken);
        if (!emailVerification.IsAllowed)
        {
            return OperationResult.Failure(emailVerification.Message);
        }

        var existing = await repository.GetVoterByEmailAsync(normalizedEmail, cancellationToken);

        if (existing is not null)
        {
            return OperationResult.Failure("An account with this email address already exists.");
        }

        var (hash, salt) = passwordHasher.HashPassword(model.Password);
        var voter = new VoterAccount
        {
            Id = ToVoterId(normalizedEmail),
            FullName = model.FullName.Trim(),
            Email = normalizedEmail,
            Province = model.Province.Trim(),
            PasswordHash = hash,
            PasswordSalt = salt,
            HasVoted = false,
            CreatedAtUtc = DateTime.UtcNow
        };

        var created = await repository.CreateVoterAsync(voter, cancellationToken);
        if (!created)
        {
            return OperationResult.Failure("The registration could not be completed because the account already exists.");
        }

        return OperationResult.Success("Registration successful. You can now cast your vote.", voter.Id, voter.Email, voter.FullName);
    }

    public async Task<OperationResult> AuthenticateAsync(LoginViewModel model, CancellationToken cancellationToken)
    {
        await repository.EnsureSeedDataAsync(cancellationToken);

        var normalizedEmail = model.Email.Trim().ToLowerInvariant();
        var voter = await repository.GetVoterByEmailAsync(normalizedEmail, cancellationToken);

        if (voter is null || !passwordHasher.Verify(model.Password, voter.PasswordHash, voter.PasswordSalt))
        {
            return OperationResult.Failure("Invalid email address or password.");
        }

        await repository.UpdateLastLoginAsync(voter.Id, DateTime.UtcNow, cancellationToken);
        return OperationResult.Success("Login successful.", voter.Id, voter.Email, voter.FullName);
    }

    public async Task<OperationResult> CastVoteAsync(BallotSubmissionViewModel model, CancellationToken cancellationToken)
    {
        await repository.EnsureSeedDataAsync(cancellationToken);

        var voterId = currentUserService.UserId;
        if (string.IsNullOrWhiteSpace(voterId))
        {
            return OperationResult.Failure("Your session has expired. Please sign in again.");
        }

        var voter = await repository.GetVoterByIdAsync(voterId, cancellationToken);
        if (voter is null)
        {
            return OperationResult.Failure("The voter account could not be found.");
        }

        if (voter.HasVoted)
        {
            return OperationResult.Failure("You have already voted. This platform only allows one vote per registered voter.");
        }

        var election = await repository.GetElectionAsync(cancellationToken);
        if (DateTime.UtcNow < election.StartsAtUtc || DateTime.UtcNow > election.EndsAtUtc)
        {
            return OperationResult.Failure("Voting is currently closed for this election.");
        }

        var success = await repository.CastVoteAsync(voterId, model.CandidateId, cancellationToken);
        return success
            ? OperationResult.Success("Your vote has been recorded successfully.")
            : OperationResult.Failure("The vote could not be recorded. Please refresh the page and try again.");
    }

    private static string ToVoterId(string email) => email.Replace("@", "_at_").Replace(".", "_");
}
