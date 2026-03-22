namespace EVotingSystem.Options;

public class MailCheckOptions
{
    public const string SectionName = "MailCheck";

    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.usercheck.com";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey);
}
