using EVotingSystem.Infrastructure.Identity;
using EVotingSystem.Models.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace EVotingSystem.Tests.Fakes;

public static class IdentityTestFactory
{
    public static UserManager<ApplicationUser> CreateUserManager(FirestoreUserStore? store = null)
    {
        store ??= new FirestoreUserStore();

        return new UserManager<ApplicationUser>(
            store,
            Microsoft.Extensions.Options.Options.Create(new IdentityOptions
            {
                Password =
                {
                    RequiredLength = 8,
                    RequireDigit = true,
                    RequireUppercase = true,
                    RequireLowercase = true,
                    RequireNonAlphanumeric = false
                },
                User =
                {
                    RequireUniqueEmail = true
                }
            }),
            new PasswordHasher<ApplicationUser>(),
            [new UserValidator<ApplicationUser>()],
            [new PasswordValidator<ApplicationUser>()],
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            new TestServiceProvider(),
            NullLogger<UserManager<ApplicationUser>>.Instance);
    }

    private sealed class TestServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }
}
