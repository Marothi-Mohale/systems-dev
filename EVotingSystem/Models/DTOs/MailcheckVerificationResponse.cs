using System.Text.Json;
using System.Text.Json.Serialization;

namespace EVotingSystem.Models.DTOs;

public class MailcheckVerificationResponse
{
    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("user")]
    public string? User { get; set; }

    [JsonPropertyName("domain")]
    public string? Domain { get; set; }

    [JsonPropertyName("did_you_mean")]
    public string? DidYouMean { get; set; }

    [JsonPropertyName("mx")]
    public bool? HasMxRecords { get; set; }

    [JsonPropertyName("disposable")]
    public bool? IsDisposable { get; set; }

    [JsonPropertyName("spam")]
    public bool? IsSpam { get; set; }

    [JsonPropertyName("valid")]
    public bool? IsValid { get; set; }

    [JsonPropertyName("syntax_valid")]
    public bool? IsSyntaxValid { get; set; }

    [JsonPropertyName("deliverable")]
    public bool? IsDeliverable { get; set; }

    [JsonPropertyName("risk")]
    public string? Risk { get; set; }

    [JsonPropertyName("risk_level")]
    public string? RiskLevel { get; set; }

    [JsonPropertyName("score")]
    public decimal? Score { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalData { get; set; }

    public string EffectiveRiskLevel =>
        !string.IsNullOrWhiteSpace(RiskLevel)
            ? RiskLevel.Trim().ToLowerInvariant()
            : !string.IsNullOrWhiteSpace(Risk)
                ? Risk.Trim().ToLowerInvariant()
                : "unknown";
}
