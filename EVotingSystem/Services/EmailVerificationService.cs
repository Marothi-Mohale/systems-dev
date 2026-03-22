using System.Net;
using System.Text.Json;
using EVotingSystem.Options;
using Microsoft.Extensions.Options;

namespace EVotingSystem.Services;

public class EmailVerificationService(IHttpClientFactory httpClientFactory, IOptions<MailCheckOptions> options)
{
    public async Task<(bool IsAllowed, string Message)> VerifyAsync(string email, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        if (!settings.IsConfigured)
        {
            return (true, "MailCheck API key not configured. Email verification is currently running in local bypass mode.");
        }

        var client = httpClientFactory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{settings.BaseUrl.TrimEnd('/')}/email/{Uri.EscapeDataString(email)}");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", settings.ApiKey);

        using var response = await client.SendAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.TooManyRequests)
        {
            return (false, "Email verification is currently rate-limited. Please try again shortly.");
        }

        if (!response.IsSuccessStatusCode)
        {
            return (false, "The email verification service could not validate this address right now.");
        }

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        var root = document.RootElement;

        var hasMx = root.TryGetProperty("mx", out var mxElement) && mxElement.GetBoolean();
        var isDisposable = root.TryGetProperty("disposable", out var disposableElement) && disposableElement.GetBoolean();
        var isSpam = root.TryGetProperty("spam", out var spamElement) && spamElement.GetBoolean();
        var suggestion = root.TryGetProperty("did_you_mean", out var suggestionElement) && suggestionElement.ValueKind != JsonValueKind.Null
            ? suggestionElement.GetString()
            : null;

        if (!hasMx || isDisposable || isSpam)
        {
            return (false, "Please register with a valid non-disposable email address.");
        }

        return suggestion is null
            ? (true, "Email address verified.")
            : (true, $"Email address verified. Suggested normalized email: {suggestion}");
    }
}
