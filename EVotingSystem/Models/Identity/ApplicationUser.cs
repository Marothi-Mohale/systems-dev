using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace EVotingSystem.Models.Identity;

public class ApplicationUser : IdentityUser
{
    [PersonalData]
    [Required]
    [StringLength(120, MinimumLength = 2)]
    public string FullName { get; set; } = string.Empty;

    [PersonalData]
    [StringLength(16)]
    [RegularExpression("^[A-Z]{2,10}$")]
    public string? ProvinceCode { get; set; }

    [PersonalData]
    [StringLength(120)]
    public string? ProvinceName { get; set; }

    [PersonalData]
    public bool HasVoted { get; set; }

    [PersonalData]
    [StringLength(64)]
    public string? ActiveElectionId { get; set; }

    public bool MailcheckValidated { get; set; }

    [StringLength(32)]
    public string MailcheckStatus { get; set; } = "unknown";

    public DateTime RegisteredAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAtUtc { get; set; }
}
