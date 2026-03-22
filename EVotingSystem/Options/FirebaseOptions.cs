namespace EVotingSystem.Options;

public class FirebaseOptions
{
    public const string SectionName = "Firebase";

    public string ProjectId { get; set; } = string.Empty;
    public string DatabaseId { get; set; } = "(default)";
    public string ServiceAccountEmail { get; set; } = string.Empty;
    public string ServiceAccountPrivateKey { get; set; } = string.Empty;
    public string TokenUri { get; set; } = "https://oauth2.googleapis.com/token";

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ProjectId) &&
        !string.IsNullOrWhiteSpace(ServiceAccountEmail) &&
        !string.IsNullOrWhiteSpace(ServiceAccountPrivateKey);
}
