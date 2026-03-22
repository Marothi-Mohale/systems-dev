using EVotingSystem.Controllers;
using EVotingSystem.Models.DTOs;
using EVotingSystem.Models.ViewModels;
using EVotingSystem.Services.Interfaces;
using EVotingSystem.Tests.Fakes;
using EVotingSystem.Tests.Testing;
using Microsoft.AspNetCore.Mvc;

namespace EVotingSystem.Tests.Controllers;

public static class AccountControllerTests
{
    public static IEnumerable<TestCase> GetCases()
    {
        yield return new TestCase(
            "Account registration does not create a user before email validation succeeds",
            BlocksRegistrationWhenEmailValidationFailsAsync);
    }

    private static async Task BlocksRegistrationWhenEmailValidationFailsAsync()
    {
        var userManager = IdentityTestFactory.CreateUserManager();
        var signInManager = new TestSignInManager(userManager);
        var emailValidation = new StubEmailValidationService(new EmailVerificationResult
        {
            IsAllowed = false,
            VerificationCompleted = true,
            NormalizedEmail = "student@example.com",
            Reason = "Please enter a valid email address.",
            RiskLevel = "invalid"
        });

        var controller = new AccountController(
            userManager,
            signInManager,
            emailValidation,
            new TestLogger<AccountController>());

        var model = new RegistrationViewModel
        {
            FullName = "Student User",
            Email = "student@example.com",
            Password = "Secure123",
            ConfirmPassword = "Secure123"
        };

        var result = await controller.Register(model, CancellationToken.None);
        var view = AssertEx.IsType<ViewResult>(result);
        var createdUser = await userManager.FindByEmailAsync("student@example.com");

        AssertEx.NotNull(view.Model);
        AssertEx.True(controller.ModelState.ContainsKey(nameof(RegistrationViewModel.Email)));
        AssertEx.True(createdUser is null, "User should not be created when email validation fails.");
        AssertEx.False(signInManager.SignInCalled, "Sign-in should not occur when registration is blocked.");
    }

    private sealed class StubEmailValidationService(EmailVerificationResult result) : IEmailValidationService
    {
        public Task<EmailVerificationResult> ValidateAsync(string email, CancellationToken cancellationToken) =>
            Task.FromResult(result);
    }
}
