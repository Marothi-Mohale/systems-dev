using System.ComponentModel.DataAnnotations;
using EVotingSystem.Models.DTOs;
using EVotingSystem.Options;
using EVotingSystem.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace EVotingSystem.Services;

public class MailcheckEmailValidationService(
    IMailcheckClient mailcheckClient,
    IOptions<MailCheckOptions> options,
    ILogger<MailcheckEmailValidationService> logger) : IEmailValidationService
{
    private static readonly EmailAddressAttribute EmailAddressValidator = new();
    private readonly MailCheckOptions mailCheckOptions = options.Value;

    public async Task<EmailVerificationResult> ValidateAsync(string email, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var normalizedEmail = email.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalizedEmail) || !EmailAddressValidator.IsValid(normalizedEmail))
        {
            return new EmailVerificationResult
            {
                IsAllowed = false,
                VerificationCompleted = true,
                NormalizedEmail = normalizedEmail,
                Reason = "Enter a valid email address.",
                RiskLevel = "invalid"
            };
        }

        if (!mailCheckOptions.IsConfigured)
        {
            logger.LogError("Mailcheck validation was requested but the API key is not configured.");
            return new EmailVerificationResult
            {
                IsAllowed = false,
                VerificationCompleted = false,
                NormalizedEmail = normalizedEmail,
                Reason = "Email verification is temporarily unavailable. Please try again later.",
                RiskLevel = "service-unavailable"
            };
        }

        try
        {
            var response = await mailcheckClient.VerifyEmailAsync(
                new MailcheckVerificationRequest { Email = normalizedEmail },
                cancellationToken);

            return Evaluate(normalizedEmail, response);
        }
        catch (MailcheckClientException exception) when (exception.FailureKind == MailcheckFailureKind.RateLimited)
        {
            logger.LogWarning("Mailcheck rate limit prevented registration verification for {Email}.", MaskEmail(normalizedEmail));
            return new EmailVerificationResult
            {
                IsAllowed = false,
                VerificationCompleted = false,
                IsRateLimited = true,
                NormalizedEmail = normalizedEmail,
                Reason = "Email verification is busy right now. Please try again in a moment.",
                RiskLevel = "rate-limited"
            };
        }
        catch (MailcheckClientException exception) when (exception.FailureKind == MailcheckFailureKind.MalformedResponse)
        {
            logger.LogWarning(exception, "Mailcheck returned a malformed response for {Email}.", MaskEmail(normalizedEmail));
            return new EmailVerificationResult
            {
                IsAllowed = false,
                VerificationCompleted = false,
                IsMalformedResponse = true,
                NormalizedEmail = normalizedEmail,
                Reason = "Email verification could not confirm this address right now. Please try again later.",
                RiskLevel = "malformed-response"
            };
        }
        catch (MailcheckClientException exception)
        {
            logger.LogWarning(exception, "Mailcheck verification failed for {Email} with failure kind {FailureKind}.", MaskEmail(normalizedEmail), exception.FailureKind);
            return new EmailVerificationResult
            {
                IsAllowed = false,
                VerificationCompleted = false,
                NormalizedEmail = normalizedEmail,
                Reason = "Email verification is temporarily unavailable. Please try again later.",
                RiskLevel = exception.FailureKind.ToString().ToLowerInvariant()
            };
        }
    }

    private EmailVerificationResult Evaluate(string normalizedEmail, MailcheckVerificationResponse response)
    {
        var riskLevel = response.EffectiveRiskLevel;
        var userFriendlyReason = GetBlockingReason(response, riskLevel);
        var isAllowed = string.IsNullOrWhiteSpace(userFriendlyReason);

        logger.LogInformation(
            "Mailcheck policy evaluation completed for {Email}. Allowed: {IsAllowed}, Risk: {RiskLevel}, Disposable: {IsDisposable}, Deliverable: {IsDeliverable}, MX: {HasMx}.",
            MaskEmail(normalizedEmail),
            isAllowed,
            riskLevel,
            response.IsDisposable,
            response.IsDeliverable,
            response.HasMxRecords);

        return new EmailVerificationResult
        {
            IsAllowed = isAllowed,
            VerificationCompleted = true,
            NormalizedEmail = normalizedEmail,
            Reason = isAllowed ? "Email address verified successfully." : userFriendlyReason!,
            RiskLevel = riskLevel,
            IsDisposable = response.IsDisposable,
            HasMxRecords = response.HasMxRecords,
            IsDeliverable = response.IsDeliverable ?? response.IsValid ?? response.IsSyntaxValid,
            SuggestedEmail = response.DidYouMean
        };
    }

    private string? GetBlockingReason(MailcheckVerificationResponse response, string riskLevel)
    {
        if (response.IsValid == false || response.IsSyntaxValid == false)
        {
            return "Please enter a valid email address.";
        }

        if (mailCheckOptions.RequireDeliverableResult && response.IsDeliverable == false)
        {
            return "Please register with a deliverable email address.";
        }

        if (mailCheckOptions.RequireMxRecords && response.HasMxRecords == false)
        {
            return "Please register with an email address that can receive mail.";
        }

        if (mailCheckOptions.RejectDisposable && response.IsDisposable == true)
        {
            return "Disposable email addresses are not allowed for registration.";
        }

        if (mailCheckOptions.RejectSpam && response.IsSpam == true)
        {
            return "This email address cannot be used for registration.";
        }

        if (mailCheckOptions.RejectRisky &&
            mailCheckOptions.RejectedRiskLevels.Any(level => string.Equals(level, riskLevel, StringComparison.OrdinalIgnoreCase)))
        {
            return "This email address was flagged as risky. Please use a different address.";
        }

        return null;
    }

    private static string MaskEmail(string email)
    {
        var atIndex = email.IndexOf('@');
        if (atIndex <= 1)
        {
            return "***";
        }

        return $"{email[..Math.Min(2, atIndex)]}***{email[atIndex..]}";
    }
}
