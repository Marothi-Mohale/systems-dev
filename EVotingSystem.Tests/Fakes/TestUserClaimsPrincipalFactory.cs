using System.Security.Claims;
using EVotingSystem.Models.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace EVotingSystem.Tests.Fakes;

public sealed class TestUserClaimsPrincipalFactory : UserClaimsPrincipalFactory<ApplicationUser>
{
    public TestUserClaimsPrincipalFactory(UserManager<ApplicationUser> userManager)
        : base(userManager, Microsoft.Extensions.Options.Options.Create(new IdentityOptions()))
    {
    }

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);
        if (!string.IsNullOrWhiteSpace(user.Email))
        {
            identity.AddClaim(new Claim(ClaimTypes.Email, user.Email));
        }

        return identity;
    }
}
