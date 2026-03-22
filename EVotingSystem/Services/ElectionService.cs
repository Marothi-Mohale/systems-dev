using EVotingSystem.Models.Domain;
using EVotingSystem.Models.ViewModels;
using EVotingSystem.Services.Interfaces;

namespace EVotingSystem.Services;

public class ElectionService(
    IElectionRepository repository,
    CurrentUserService currentUserService,
    IEmailValidationService emailValidationService)
{
    public async Task<PublicResultsViewModel> GetDashboardAsync(CancellationToken cancellationToken)
    {
        await repository.EnsureSeedDataAsync(cancellationToken);

        var election = await repository.GetElectionAsync(cancellationToken);
        var candidates = await repository.GetCandidatesAsync(cancellationToken);
        var totalVotes = candidates.Sum(candidate => candidate.VoteCount);

        var results = candidates
            .Select(candidate => new CandidateResultViewModel
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

        var maxVotes = results.Count == 0 ? 0 : results.Max(candidate => candidate.VoteCount);
        foreach (var candidate in results)
        {
            candidate.IsLeading = candidate.VoteCount == maxVotes && maxVotes > 0;
        }

        return new PublicResultsViewModel
        {
            Election = election,
            CandidateResults = results,
            Statistics = new PollStatistics
            {
                ElectionId = election.Id,
                TotalVotesCast = totalVotes,
                AcceptedVotes = totalVotes,
                EligibleVoterCount = election.TotalPopulation,
                DistinctVoterCount = totalVotes,
                ElectionOpen = election.IsVotingOpen(DateTime.UtcNow),
                GeneratedAtUtc = DateTime.UtcNow,
                VotingRules = election.VotingRules
            }
        };
    }

    public async Task<VoteViewModel> GetBallotAsync(CancellationToken cancellationToken)
    {
        await repository.EnsureSeedDataAsync(cancellationToken);

        var election = await repository.GetElectionAsync(cancellationToken);
        var candidates = await repository.GetCandidatesAsync(cancellationToken);
        var voterId = currentUserService.UserId;
        var voter = string.IsNullOrWhiteSpace(voterId) ? null : await repository.GetVoterByIdAsync(voterId, cancellationToken);

        return new VoteViewModel
        {
            Election = election,
            Candidates = candidates,
            AlreadyVoted = voter?.HasVoted == true,
            SelectedCandidateId = voter?.SelectedCandidateId,
            VoterName = currentUserService.FullName ?? voter?.FullName ?? "Voter",
            VotingRules = election.VotingRules
        };
    }

    public async Task<OperationResult> RegisterVoterAsync(RegistrationViewModel model, CancellationToken cancellationToken)
    {
        await repository.EnsureSeedDataAsync(cancellationToken);

        var normalizedEmail = model.Email.Trim().ToLowerInvariant();
        var emailVerification = await emailValidationService.ValidateAsync(normalizedEmail, cancellationToken);
        if (!emailVerification.IsAllowed)
        {
            return OperationResult.Failure(emailVerification.Reason);
        }

        var existing = await repository.GetVoterByEmailAsync(normalizedEmail, cancellationToken);

        if (existing is not null)
        {
            return OperationResult.Failure("An account with this email address already exists.");
        }

        var voter = new VoterProfile
        {
            Id = ToVoterId(normalizedEmail),
            ApplicationUserId = ToVoterId(normalizedEmail),
            FullName = model.FullName.Trim(),
            Email = normalizedEmail,
            ProvinceCode = model.ProvinceCode,
            ProvinceName = model.ProvinceName,
            HasVoted = false,
            RegisteredAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
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

        var passwordLooksValid = !string.IsNullOrWhiteSpace(model.Password);
        if (voter is null || !passwordLooksValid)
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
        if (!election.IsVotingOpen(DateTime.UtcNow))
        {
            return OperationResult.Failure("Voting is currently closed for this election.");
        }

        var success = await repository.CastVoteAsync(voterId, model.CandidateId, election.Id, cancellationToken);
        return success
            ? OperationResult.Success("Your vote has been recorded successfully.")
            : OperationResult.Failure("The vote could not be recorded. Please refresh the page and try again.");
    }

    private static string ToVoterId(string email) => email.Replace("@", "_at_").Replace(".", "_");
}
