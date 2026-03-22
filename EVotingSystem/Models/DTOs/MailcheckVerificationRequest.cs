using System.ComponentModel.DataAnnotations;

namespace EVotingSystem.Models.DTOs;

public class MailcheckVerificationRequest
{
    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;
}
