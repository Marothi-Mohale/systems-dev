using System.ComponentModel.DataAnnotations;

namespace EVotingSystem.Models.ViewModels;

public class RegistrationViewModel
{
    [Required]
    [StringLength(120, MinimumLength = 2)]
    [RegularExpression(@"^[\p{L}\p{M}][\p{L}\p{M}\p{N}\-'\. ]*$", ErrorMessage = "Full name contains unsupported characters.")]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [StringLength(16)]
    [Display(Name = "Province")]
    [RegularExpression("^[A-Z]{2,10}$", ErrorMessage = "Province codes use uppercase abbreviations like GP or WC.")]
    public string? ProvinceCode { get; set; }

    [StringLength(120)]
    public string? ProvinceName { get; set; }

    [Required]
    [DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 8)]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$", ErrorMessage = "Password must include upper-case, lower-case, and a number.")]
    public string Password { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Compare(nameof(Password))]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
