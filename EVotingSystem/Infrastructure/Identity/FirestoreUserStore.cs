using System.Collections.Concurrent;
using EVotingSystem.Models.Identity;
using Microsoft.AspNetCore.Identity;

namespace EVotingSystem.Infrastructure.Identity;

public class FirestoreUserStore :
    IUserStore<ApplicationUser>,
    IUserEmailStore<ApplicationUser>,
    IUserPasswordStore<ApplicationUser>,
    IUserSecurityStampStore<ApplicationUser>
{
    private readonly ConcurrentDictionary<string, ApplicationUser> usersById = new();

    public Task<IdentityResult> CreateAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        user.Id = string.IsNullOrWhiteSpace(user.Id) ? Guid.NewGuid().ToString("N") : user.Id;
        user.RegisteredAtUtc = user.RegisteredAtUtc == default ? DateTime.UtcNow : user.RegisteredAtUtc;
        user.UpdatedAtUtc = DateTime.UtcNow;

        if (usersById.Values.Any(existing =>
                string.Equals(existing.NormalizedEmail, user.NormalizedEmail, StringComparison.Ordinal) ||
                string.Equals(existing.NormalizedUserName, user.NormalizedUserName, StringComparison.Ordinal)))
        {
            return Task.FromResult(IdentityResult.Failed(
                new IdentityError
                {
                    Code = "DuplicateUser",
                    Description = "An account with this email address already exists."
                }));
        }

        if (!usersById.TryAdd(user.Id, Clone(user)))
        {
            return Task.FromResult(IdentityResult.Failed(new IdentityError
            {
                Description = "The user could not be created."
            }));
        }

        return Task.FromResult(IdentityResult.Success);
    }

    public Task<IdentityResult> DeleteAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        usersById.TryRemove(user.Id, out _);
        return Task.FromResult(IdentityResult.Success);
    }

    public void Dispose()
    {
    }

    public Task<ApplicationUser?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var user = usersById.Values.FirstOrDefault(candidate =>
            string.Equals(candidate.NormalizedEmail, normalizedEmail, StringComparison.Ordinal));

        return Task.FromResult(user is null ? null : Clone(user));
    }

    public Task<ApplicationUser?> FindByIdAsync(string userId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(usersById.TryGetValue(userId, out var user) ? Clone(user) : null);
    }

    public Task<ApplicationUser?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var user = usersById.Values.FirstOrDefault(candidate =>
            string.Equals(candidate.NormalizedUserName, normalizedUserName, StringComparison.Ordinal));

        return Task.FromResult(user is null ? null : Clone(user));
    }

    public Task<string?> GetEmailAsync(ApplicationUser user, CancellationToken cancellationToken)
        => Task.FromResult(user.Email);

    public Task<bool> GetEmailConfirmedAsync(ApplicationUser user, CancellationToken cancellationToken)
        => Task.FromResult(user.EmailConfirmed);

    public Task<string?> GetNormalizedEmailAsync(ApplicationUser user, CancellationToken cancellationToken)
        => Task.FromResult(user.NormalizedEmail);

    public Task<string?> GetNormalizedUserNameAsync(ApplicationUser user, CancellationToken cancellationToken)
        => Task.FromResult(user.NormalizedUserName);

    public Task<string?> GetPasswordHashAsync(ApplicationUser user, CancellationToken cancellationToken)
        => Task.FromResult(user.PasswordHash);

    public Task<string?> GetSecurityStampAsync(ApplicationUser user, CancellationToken cancellationToken)
        => Task.FromResult(user.SecurityStamp);

    public Task<string> GetUserIdAsync(ApplicationUser user, CancellationToken cancellationToken)
        => Task.FromResult(user.Id);

    public Task<string?> GetUserNameAsync(ApplicationUser user, CancellationToken cancellationToken)
        => Task.FromResult(user.UserName);

    public Task<bool> HasPasswordAsync(ApplicationUser user, CancellationToken cancellationToken)
        => Task.FromResult(!string.IsNullOrWhiteSpace(user.PasswordHash));

    public Task SetEmailAsync(ApplicationUser user, string? email, CancellationToken cancellationToken)
    {
        user.Email = email?.Trim();
        return Task.CompletedTask;
    }

    public Task SetEmailConfirmedAsync(ApplicationUser user, bool confirmed, CancellationToken cancellationToken)
    {
        user.EmailConfirmed = confirmed;
        return Task.CompletedTask;
    }

    public Task SetNormalizedEmailAsync(ApplicationUser user, string? normalizedEmail, CancellationToken cancellationToken)
    {
        user.NormalizedEmail = normalizedEmail;
        return Task.CompletedTask;
    }

    public Task SetNormalizedUserNameAsync(ApplicationUser user, string? normalizedName, CancellationToken cancellationToken)
    {
        user.NormalizedUserName = normalizedName;
        return Task.CompletedTask;
    }

    public Task SetPasswordHashAsync(ApplicationUser user, string? passwordHash, CancellationToken cancellationToken)
    {
        user.PasswordHash = passwordHash;
        return Task.CompletedTask;
    }

    public Task SetSecurityStampAsync(ApplicationUser user, string stamp, CancellationToken cancellationToken)
    {
        user.SecurityStamp = stamp;
        return Task.CompletedTask;
    }

    public Task SetUserNameAsync(ApplicationUser user, string? userName, CancellationToken cancellationToken)
    {
        user.UserName = userName?.Trim();
        return Task.CompletedTask;
    }

    public Task<IdentityResult> UpdateAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        user.UpdatedAtUtc = DateTime.UtcNow;
        usersById[user.Id] = Clone(user);
        return Task.FromResult(IdentityResult.Success);
    }

    private static ApplicationUser Clone(ApplicationUser user) =>
        new()
        {
            Id = user.Id,
            UserName = user.UserName,
            NormalizedUserName = user.NormalizedUserName,
            Email = user.Email,
            NormalizedEmail = user.NormalizedEmail,
            EmailConfirmed = user.EmailConfirmed,
            PasswordHash = user.PasswordHash,
            SecurityStamp = user.SecurityStamp,
            ConcurrencyStamp = user.ConcurrencyStamp,
            PhoneNumber = user.PhoneNumber,
            LockoutEnabled = user.LockoutEnabled,
            AccessFailedCount = user.AccessFailedCount,
            FullName = user.FullName,
            ProvinceCode = user.ProvinceCode,
            ProvinceName = user.ProvinceName,
            HasVoted = user.HasVoted,
            ActiveElectionId = user.ActiveElectionId,
            MailcheckValidated = user.MailcheckValidated,
            MailcheckStatus = user.MailcheckStatus,
            RegisteredAtUtc = user.RegisteredAtUtc,
            UpdatedAtUtc = user.UpdatedAtUtc,
            LastLoginAtUtc = user.LastLoginAtUtc
        };
}
