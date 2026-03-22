using System.ComponentModel.DataAnnotations;

namespace EVotingSystem.Models.ViewModels;

public class RegisterViewModel
{
    [Required]
    [StringLength(100)]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(40)]
    public string Province { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [MinLength(8)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Compare(nameof(Password))]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
