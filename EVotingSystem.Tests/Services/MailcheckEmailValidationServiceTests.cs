using EVotingSystem.Models.DTOs;
using EVotingSystem.Options;
using EVotingSystem.Services;
using EVotingSystem.Tests.Fakes;
using EVotingSystem.Tests.Testing;
using Microsoft.Extensions.Options;

namespace EVotingSystem.Tests.Services;

public static class MailcheckEmailValidationServiceTests
{
    public static IEnumerable<TestCase> GetCases()
    {
        yield return new TestCase(
            "Email validation rejects malformed email before registration",
            RejectsMalformedEmailAsync);
        yield return new TestCase(
            "Email validation blocks disposable email addresses",
            RejectsDisposableEmailAsync);
    }

    private static async Task RejectsMalformedEmailAsync()
    {
        var service = CreateService(new FakeMailcheckClient());
        var result = await service.ValidateAsync("not-an-email", CancellationToken.None);

        AssertEx.False(result.IsAllowed);
        AssertEx.True(result.VerificationCompleted);
        AssertEx.Equal("invalid", result.RiskLevel);
        AssertEx.Contains("valid email", result.Reason);
    }

    private static async Task RejectsDisposableEmailAsync()
    {
        var client = new FakeMailcheckClient
        {
            OnVerifyAsync = (_, _) => Task.FromResult(new MailcheckVerificationResponse
            {
                IsValid = true,
                IsSyntaxValid = true,
                IsDeliverable = true,
                HasMxRecords = true,
                IsDisposable = true,
                RiskLevel = "low"
            })
        };

        var service = CreateService(client);
        var result = await service.ValidateAsync("student@example.edu", CancellationToken.None);

        AssertEx.False(result.IsAllowed);
        AssertEx.True(result.VerificationCompleted);
        AssertEx.Contains("Disposable", result.Reason);
    }

    private static MailcheckEmailValidationService CreateService(FakeMailcheckClient client) =>
        new(
            client,
            Microsoft.Extensions.Options.Options.Create(new MailCheckOptions
            {
                ApiKey = "test-key",
                RejectDisposable = true,
                RequireDeliverableResult = true,
                RequireMxRecords = true,
                RejectRisky = true,
                RejectSpam = true
            }),
            new TestLogger<MailcheckEmailValidationService>());
}
