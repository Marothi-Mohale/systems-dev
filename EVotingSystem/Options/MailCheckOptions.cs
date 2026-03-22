namespace EVotingSystem.Options;

public class MailCheckOptions
{
    public const string SectionName = "MailCheck";

    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.mailcheck.ai";
    public string VerifyEndpointTemplate { get; set; } = "/email/{email}";
    public int TimeoutSeconds { get; set; } = 10;
    public int MaxAttempts { get; set; } = 2;
    public int RetryBaseDelayMilliseconds { get; set; } = 250;
    public bool RequireMxRecords { get; set; } = true;
    public bool RequireDeliverableResult { get; set; } = true;
    public bool RejectDisposable { get; set; } = true;
    public bool RejectSpam { get; set; } = true;
    public bool RejectRisky { get; set; } = true;
    public string[] RejectedRiskLevels { get; set; } = ["high", "risky", "spam", "malicious", "invalid", "disposable"];

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey);
}
