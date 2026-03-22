using EVotingSystem.Models.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EVotingSystem.Tests.Fakes;

public sealed class TestSignInManager : SignInManager<ApplicationUser>
{
    public bool SignInCalled { get; private set; }

    public TestSignInManager(UserManager<ApplicationUser> userManager)
        : base(
            userManager,
            new HttpContextAccessor { HttpContext = new DefaultHttpContext() },
            new TestUserClaimsPrincipalFactory(userManager),
            Microsoft.Extensions.Options.Options.Create(new IdentityOptions()),
            new Logger<SignInManager<ApplicationUser>>(new LoggerFactory()),
            new AuthenticationSchemeProvider(Microsoft.Extensions.Options.Options.Create(new AuthenticationOptions())),
            new TestUserConfirmation())
    {
    }

    public override Task SignInAsync(ApplicationUser user, bool isPersistent, string? authenticationMethod = null)
    {
        SignInCalled = true;
        return Task.CompletedTask;
    }
}
