namespace EVotingSystem.Models.DTOs;

public class EmailVerificationResult
{
    public bool IsAllowed { get; set; }
    public bool VerificationCompleted { get; set; }
    public bool IsRateLimited { get; set; }
    public bool IsMalformedResponse { get; set; }
    public string NormalizedEmail { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string RiskLevel { get; set; } = "unknown";
    public bool? IsDisposable { get; set; }
    public bool? HasMxRecords { get; set; }
    public bool? IsDeliverable { get; set; }
    public string? SuggestedEmail { get; set; }
}
