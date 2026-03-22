using EVotingSystem.Models.DTOs;
using EVotingSystem.Options;
using EVotingSystem.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace EVotingSystem.Services;

public class MailcheckEmailValidationService(IOptions<MailCheckOptions> options) : IEmailValidationService
{
    private readonly MailCheckOptions mailCheckOptions = options.Value;

    public Task<EmailVerificationResult> ValidateAsync(string email, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // TODO: Replace this placeholder with a real Mailcheck.ai HTTP client.
        // The registration flow should call the API before creating the user.
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var isConfigured = mailCheckOptions.IsConfigured;

        return Task.FromResult(new EmailVerificationResult
        {
            IsAllowed = !string.IsNullOrWhiteSpace(normalizedEmail),
            NormalizedEmail = normalizedEmail,
            Reason = isConfigured
                ? "Mailcheck.ai integration placeholder is configured and ready for implementation."
                : "Mailcheck.ai API key is not configured. Development placeholder response returned.",
            RiskLevel = isConfigured ? "pending-live-check" : "development-bypass"
        });
    }
}
