using EVotingSystem.Models.Identity;
using Microsoft.AspNetCore.Identity;

namespace EVotingSystem.Tests.Fakes;

public sealed class TestUserConfirmation : IUserConfirmation<ApplicationUser>
{
    public Task<bool> IsConfirmedAsync(UserManager<ApplicationUser> manager, ApplicationUser user) =>
        Task.FromResult(true);
}
