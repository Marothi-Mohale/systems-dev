using Microsoft.AspNetCore.Identity;

namespace EVotingSystem.Models.Identity;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public string ProvinceCode { get; set; } = string.Empty;
    public bool HasVoted { get; set; }
    public bool MailcheckValidated { get; set; }
    public string MailcheckStatus { get; set; } = string.Empty;
    public DateTime RegisteredAtUtc { get; set; } = DateTime.UtcNow;
}
