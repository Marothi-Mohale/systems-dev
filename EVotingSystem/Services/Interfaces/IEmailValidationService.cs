using EVotingSystem.Models.DTOs;

namespace EVotingSystem.Services.Interfaces;

public interface IEmailValidationService
{
    Task<EmailVerificationResult> ValidateAsync(string email, CancellationToken cancellationToken);
}
