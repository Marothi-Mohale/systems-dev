namespace EVotingSystem.Models.DTOs;

public class EmailVerificationResult
{
    public bool IsAllowed { get; set; }
    public string NormalizedEmail { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string RiskLevel { get; set; } = "unknown";
}
