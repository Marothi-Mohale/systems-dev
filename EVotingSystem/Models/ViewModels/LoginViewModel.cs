using System.ComponentModel.DataAnnotations;

namespace EVotingSystem.Models.ViewModels;

public class LoginViewModel
{
    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 8)]
    public string Password { get; set; } = string.Empty;

    [StringLength(2048)]
    public string? ReturnUrl { get; set; }
}
