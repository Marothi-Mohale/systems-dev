using EVotingSystem.Models.DTOs;

namespace EVotingSystem.Services.Interfaces;

public interface IMailcheckClient
{
    Task<MailcheckVerificationResponse> VerifyEmailAsync(MailcheckVerificationRequest request, CancellationToken cancellationToken);
}
