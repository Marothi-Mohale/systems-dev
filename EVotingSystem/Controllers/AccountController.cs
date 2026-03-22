using EVotingSystem.Models.Identity;
using EVotingSystem.Models.ViewModels;
using EVotingSystem.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

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

    [HttpGet]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }

        return View(new RegistrationViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegistrationViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var fullName = model.FullName.Trim();
        var provinceCode = model.ProvinceCode?.Trim().ToUpperInvariant();
        var provinceName = ResolveProvinceName(provinceCode);

        try
        {
            var emailCheck = await emailValidationService.ValidateAsync(model.Email, cancellationToken);
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
                FullName = fullName,
                ProvinceCode = provinceCode,
                ProvinceName = provinceName,
                MailcheckValidated = true,
                MailcheckStatus = emailCheck.RiskLevel
            };

            var createResult = await userManager.CreateAsync(user, model.Password);
            if (!createResult.Succeeded)
            {
                foreach (var error in createResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                logger.LogWarning("User registration failed for {Email}.", model.Email);
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
            logger.LogError(exception, "Registration failed unexpectedly for {Email}.", model.Email);
            ModelState.AddModelError(string.Empty, "We could not complete registration right now. Please try again.");
            return View(model);
        }
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        cancellationToken.ThrowIfCancellationRequested();

        var user = await userManager.FindByEmailAsync(model.Email.Trim());
        if (user is null)
        {
            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            logger.LogWarning("Invalid login attempt for {Email}.", model.Email);
            return View(model);
        }

        var result = await signInManager.CheckPasswordSignInAsync(user, model.Password, lockoutOnFailure: false);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            logger.LogWarning("Invalid login attempt for {Email}.", model.Email);
            return View(model);
        }

        user.LastLoginAtUtc = DateTime.UtcNow;
        await userManager.UpdateAsync(user);
        await signInManager.SignInAsync(user, isPersistent: false);

        if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
        {
            return Redirect(model.ReturnUrl);
        }

        TempData["StatusMessage"] = "You have been signed in successfully.";
        return RedirectToAction("Ballot", "Election");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        TempData["StatusMessage"] = "You have been signed out.";
        return RedirectToAction("Index", "Home");
    }

    private static string? ResolveProvinceName(string? provinceCode) =>
        !string.IsNullOrWhiteSpace(provinceCode) && ProvinceNames.TryGetValue(provinceCode, out var provinceName)
            ? provinceName
            : null;
}
