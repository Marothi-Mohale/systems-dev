using EVotingSystem.Models.Identity;
using EVotingSystem.Models.ViewModels;
using EVotingSystem.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace EVotingSystem.Controllers;

public class AccountController(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    IEmailValidationService emailValidationService,
    ILogger<AccountController> logger) : Controller
{
    private static readonly IReadOnlyDictionary<string, string> ProvinceNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["EC"] = "Eastern Cape",
        ["FS"] = "Free State",
        ["GP"] = "Gauteng",
        ["KZN"] = "KwaZulu-Natal",
        ["LP"] = "Limpopo",
        ["MP"] = "Mpumalanga",
        ["NW"] = "North West",
        ["NC"] = "Northern Cape",
        ["WC"] = "Western Cape"
    };

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }

        return View(new RegistrationViewModel());
    }

    [AllowAnonymous]
    [HttpPost]
    [EnableRateLimiting("auth-post")]
    public async Task<IActionResult> Register(RegistrationViewModel model, CancellationToken cancellationToken)
    {
        NormalizeRegistrationModel(model);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var fullName = model.FullName;
        var normalizedEmail = model.Email;
        var provinceCode = model.ProvinceCode;
        var provinceName = ResolveProvinceName(provinceCode);
        if (!string.IsNullOrWhiteSpace(provinceCode) && provinceName is null)
        {
            ModelState.AddModelError(nameof(model.ProvinceCode), "Select a supported province code.");
            return View(model);
        }

        try
        {
            var emailCheck = await emailValidationService.ValidateAsync(normalizedEmail, cancellationToken);
            if (!emailCheck.IsAllowed)
            {
                ModelState.AddModelError(nameof(model.Email), emailCheck.Reason);
                return View(model);
            }

            var existingUser = await userManager.FindByEmailAsync(emailCheck.NormalizedEmail);
            if (existingUser is not null)
            {
                ModelState.AddModelError(nameof(model.Email), "An account with this email address already exists.");
                return View(model);
            }

            var user = new ApplicationUser
            {
                UserName = emailCheck.NormalizedEmail,
                Email = emailCheck.NormalizedEmail,
                EmailConfirmed = true,
                FullName = fullName,
                ProvinceCode = provinceCode,
                ProvinceName = provinceName,
                MailcheckValidated = true,
                MailcheckStatus = emailCheck.RiskLevel,
                LockoutEnabled = true
            };

            var createResult = await userManager.CreateAsync(user, model.Password);
            if (!createResult.Succeeded)
            {
                AddRegistrationErrors(createResult);
                logger.LogWarning("User registration failed for {Email}.", MaskEmail(normalizedEmail));
                return View(model);
            }

            await signInManager.SignInAsync(user, isPersistent: false);
            logger.LogInformation("User {UserId} registered successfully.", user.Id);

            TempData["StatusMessage"] = "Your account has been created and you are now signed in.";
            return RedirectToAction("Ballot", "Election");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Registration failed unexpectedly for {Email}.", MaskEmail(normalizedEmail));
            ModelState.AddModelError(string.Empty, "We could not complete registration right now. Please try again.");
            return View(model);
        }
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }

        return View(new LoginViewModel { ReturnUrl = NormalizeReturnUrl(returnUrl) });
    }

    [AllowAnonymous]
    [HttpPost]
    [EnableRateLimiting("auth-post")]
    public async Task<IActionResult> Login(LoginViewModel model, CancellationToken cancellationToken)
    {
        model.Email = NormalizeEmail(model.Email);
        model.ReturnUrl = NormalizeReturnUrl(model.ReturnUrl);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        cancellationToken.ThrowIfCancellationRequested();

        var user = await userManager.FindByEmailAsync(model.Email);
        if (user is null)
        {
            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            logger.LogWarning("Invalid login attempt for {Email}.", MaskEmail(model.Email));
            return View(model);
        }

        var result = await signInManager.CheckPasswordSignInAsync(user, model.Password, lockoutOnFailure: false);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            logger.LogWarning("Invalid login attempt for {Email}.", MaskEmail(model.Email));
            return View(model);
        }

        user.LastLoginAtUtc = DateTime.UtcNow;
        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            logger.LogWarning("User {UserId} signed in but the last-login timestamp could not be updated.", user.Id);
        }

        await signInManager.SignInAsync(user, isPersistent: false);

        if (!string.IsNullOrWhiteSpace(model.ReturnUrl))
        {
            return Redirect(model.ReturnUrl);
        }

        TempData["StatusMessage"] = "You have been signed in successfully.";
        return RedirectToAction("Ballot", "Election");
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        TempData["StatusMessage"] = "You have been signed out.";
        return RedirectToAction("Index", "Home");
    }

    private void AddRegistrationErrors(IdentityResult createResult)
    {
        foreach (var error in createResult.Errors)
        {
            if (string.Equals(error.Code, "DuplicateUser", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(error.Code, "DuplicateEmail", StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(nameof(RegistrationViewModel.Email), "We couldn't create an account with that email address. If you already registered, please sign in instead.");
                continue;
            }

            ModelState.AddModelError(string.Empty, error.Description);
        }
    }

    private static void NormalizeRegistrationModel(RegistrationViewModel model)
    {
        model.FullName = NormalizeFullName(model.FullName);
        model.Email = NormalizeEmail(model.Email);
        model.ProvinceCode = string.IsNullOrWhiteSpace(model.ProvinceCode)
            ? null
            : model.ProvinceCode.Trim().ToUpperInvariant();
        model.ProvinceName = null;
    }

    private static string NormalizeFullName(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            return string.Empty;
        }

        return string.Join(' ', fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
    }

    private static string NormalizeEmail(string? email) =>
        string.IsNullOrWhiteSpace(email)
            ? string.Empty
            : email.Trim().ToLowerInvariant();

    private string? NormalizeReturnUrl(string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl))
        {
            return null;
        }

        if (Url.IsLocalUrl(returnUrl))
        {
            return returnUrl;
        }

        logger.LogWarning("Discarded non-local return URL during authentication flow.");
        return null;
    }

    private static string MaskEmail(string email)
    {
        var normalized = NormalizeEmail(email);
        var atIndex = normalized.IndexOf('@');
        if (atIndex <= 1)
        {
            return "***";
        }

        return $"{normalized[..Math.Min(2, atIndex)]}***{normalized[atIndex..]}";
    }

    private static string? ResolveProvinceName(string? provinceCode) =>
        !string.IsNullOrWhiteSpace(provinceCode) && ProvinceNames.TryGetValue(provinceCode, out var provinceName)
            ? provinceName
            : null;
}
