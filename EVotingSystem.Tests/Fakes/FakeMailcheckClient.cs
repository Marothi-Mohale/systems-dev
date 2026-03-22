using EVotingSystem.Models.DTOs;
using EVotingSystem.Services.Interfaces;

namespace EVotingSystem.Tests.Fakes;

public sealed class FakeMailcheckClient : IMailcheckClient
{
    public Func<MailcheckVerificationRequest, CancellationToken, Task<MailcheckVerificationResponse>>? OnVerifyAsync { get; set; }

    public Task<MailcheckVerificationResponse> VerifyEmailAsync(MailcheckVerificationRequest request, CancellationToken cancellationToken) =>
        OnVerifyAsync?.Invoke(request, cancellationToken)
        ?? Task.FromResult(new MailcheckVerificationResponse
        {
            IsValid = true,
            IsSyntaxValid = true,
            IsDeliverable = true,
            HasMxRecords = true,
            RiskLevel = "low"
        });
}
