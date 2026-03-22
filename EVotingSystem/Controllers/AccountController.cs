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

        var emailCheck = await emailValidationService.ValidateAsync(model.Email, cancellationToken);
        if (!emailCheck.IsAllowed)
        {
            ModelState.AddModelError(nameof(model.Email), emailCheck.Reason);
            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = emailCheck.NormalizedEmail,
            Email = emailCheck.NormalizedEmail,
            FullName = model.FullName,
            ProvinceCode = model.ProvinceCode,
            ProvinceName = model.ProvinceName,
            MailcheckValidated = emailCheck.IsAllowed,
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

        await signInManager.SignInAsync(user, isPersistent: true);
        logger.LogInformation("User {UserId} registered successfully.", user.Id);

        TempData["StatusMessage"] = "Registration scaffold completed. Firestore profile persistence will plug in here next.";
        return RedirectToAction("Ballot", "Election");
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

        var result = await signInManager.PasswordSignInAsync(model.Email, model.Password, isPersistent: true, lockoutOnFailure: false);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            logger.LogWarning("Invalid login attempt for {Email}.", model.Email);
            return View(model);
        }

        if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
        {
            return Redirect(model.ReturnUrl);
        }

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
}
