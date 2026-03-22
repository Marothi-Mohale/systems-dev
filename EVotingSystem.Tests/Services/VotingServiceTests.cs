using EVotingSystem.Models.Identity;
using EVotingSystem.Models.ViewModels;
using EVotingSystem.Services;
using EVotingSystem.Tests.Fakes;
using EVotingSystem.Tests.Testing;

namespace EVotingSystem.Tests.Services;

public static class VotingServiceTests
{
    public static IEnumerable<TestCase> GetCases()
    {
        yield return new TestCase(
            "Voting service blocks users who already voted",
            BlocksDuplicateVoteFromIdentityStateAsync);
        yield return new TestCase(
            "Voting service marks user as voted after successful submission",
            MarksUserAsVotedAfterSuccessfulSubmissionAsync);
    }

    private static async Task BlocksDuplicateVoteFromIdentityStateAsync()
    {
        var userManager = IdentityTestFactory.CreateUserManager();
        var user = new ApplicationUser
        {
            UserName = "voter@example.com",
            Email = "voter@example.com",
            FullName = "Existing Voter",
            HasVoted = true,
            EmailConfirmed = true
        };

        var create = await userManager.CreateAsync(user, "Secure123");
        AssertEx.True(create.Succeeded, "Test user creation should succeed.");

        var repository = new FakeFirestoreElectionRepository();
        var service = new VotingService(repository, userManager, new TestLogger<VotingService>());

        var result = await service.SubmitVoteAsync(user.Id, "candidate-1", CancellationToken.None);

        AssertEx.False(result.Succeeded);
        AssertEx.Contains("already submitted", result.Message);
        AssertEx.Equal(0, repository.SubmitVoteCallCount);
    }

    private static async Task MarksUserAsVotedAfterSuccessfulSubmissionAsync()
    {
        var userManager = IdentityTestFactory.CreateUserManager();
        var user = new ApplicationUser
        {
            UserName = "newvoter@example.com",
            Email = "newvoter@example.com",
            FullName = "New Voter",
            EmailConfirmed = true
        };

        var create = await userManager.CreateAsync(user, "Secure123");
        AssertEx.True(create.Succeeded, "Test user creation should succeed.");

        var repository = new FakeFirestoreElectionRepository
        {
            OnSubmitVoteAsync = (_, _, _) => Task.FromResult(OperationResult.Success("Your vote has been recorded successfully."))
        };

        var service = new VotingService(repository, userManager, new TestLogger<VotingService>());
        var result = await service.SubmitVoteAsync(user.Id, "candidate-1", CancellationToken.None);
        var updatedUser = await userManager.FindByIdAsync(user.Id);

        AssertEx.True(result.Succeeded);
        AssertEx.Equal(1, repository.SubmitVoteCallCount);
        AssertEx.NotNull(updatedUser);
        AssertEx.True(updatedUser!.HasVoted);
    }
}
