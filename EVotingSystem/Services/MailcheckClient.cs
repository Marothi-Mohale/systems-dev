using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using EVotingSystem.Models.DTOs;
using EVotingSystem.Options;
using EVotingSystem.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace EVotingSystem.Services;

public class MailcheckClient(
    IHttpClientFactory httpClientFactory,
    IOptions<MailCheckOptions> options,
    ILogger<MailcheckClient> logger) : IMailcheckClient
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly MailCheckOptions mailCheckOptions = options.Value;

    public async Task<MailcheckVerificationResponse> VerifyEmailAsync(MailcheckVerificationRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!mailCheckOptions.IsConfigured)
        {
            throw new MailcheckClientException(MailcheckFailureKind.NotConfigured, "Mailcheck API key is not configured.");
        }

        var maskedEmail = MaskEmail(request.Email);
        var uri = BuildVerificationUri(request.Email);

        for (var attempt = 1; attempt <= Math.Max(1, mailCheckOptions.MaxAttempts); attempt++)
        {
            try
            {
                using var httpRequest = new HttpRequestMessage(HttpMethod.Get, uri);
                httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", mailCheckOptions.ApiKey);

                logger.LogInformation("Submitting Mailcheck verification attempt {Attempt} for {Email}.", attempt, maskedEmail);

                using var response = await httpClientFactory.CreateClient("Mailcheck").SendAsync(httpRequest, cancellationToken);
                if (response.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    if (attempt < mailCheckOptions.MaxAttempts)
                    {
                        await DelayBeforeRetryAsync(attempt, response.Headers.RetryAfter?.Delta, cancellationToken);
                        continue;
                    }

                    throw new MailcheckClientException(MailcheckFailureKind.RateLimited, "Mailcheck request was rate limited.", response.StatusCode);
                }

                if ((int)response.StatusCode >= 500)
                {
                    if (attempt < mailCheckOptions.MaxAttempts)
                    {
                        await DelayBeforeRetryAsync(attempt, response.Headers.RetryAfter?.Delta, cancellationToken);
                        continue;
                    }

                    throw new MailcheckClientException(MailcheckFailureKind.ServiceUnavailable, "Mailcheck service is currently unavailable.", response.StatusCode);
                }

                if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                {
                    throw new MailcheckClientException(MailcheckFailureKind.Unauthorized, "Mailcheck credentials were rejected.", response.StatusCode);
                }

                if (!response.IsSuccessStatusCode)
                {
                    throw new MailcheckClientException(MailcheckFailureKind.UnexpectedResponse, "Mailcheck returned an unexpected response.", response.StatusCode);
                }

                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var parsed = JsonSerializer.Deserialize<MailcheckVerificationResponse>(content, SerializerOptions);
                if (parsed is null || IsMissingDecisionData(parsed))
                {
                    throw new MailcheckClientException(MailcheckFailureKind.MalformedResponse, "Mailcheck response could not be interpreted.");
                }

                logger.LogInformation(
                    "Mailcheck verification completed for {Email} with risk level {RiskLevel}.",
                    maskedEmail,
                    parsed.EffectiveRiskLevel);

                return parsed;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (MailcheckClientException)
            {
                throw;
            }
            catch (TaskCanceledException exception)
            {
                if (attempt < mailCheckOptions.MaxAttempts)
                {
                    await DelayBeforeRetryAsync(attempt, retryAfter: null, cancellationToken);
                    continue;
                }

                logger.LogWarning(exception, "Mailcheck verification timed out for {Email}.", maskedEmail);
                throw new MailcheckClientException(MailcheckFailureKind.Timeout, "Mailcheck verification timed out.", innerException: exception);
            }
            catch (JsonException exception)
            {
                logger.LogWarning(exception, "Mailcheck returned malformed JSON for {Email}.", maskedEmail);
                throw new MailcheckClientException(MailcheckFailureKind.MalformedResponse, "Mailcheck response JSON was malformed.", innerException: exception);
            }
            catch (HttpRequestException exception)
            {
                if (attempt < mailCheckOptions.MaxAttempts)
                {
                    await DelayBeforeRetryAsync(attempt, retryAfter: null, cancellationToken);
                    continue;
                }

                logger.LogWarning(exception, "Mailcheck request failed for {Email}.", maskedEmail);
                throw new MailcheckClientException(MailcheckFailureKind.ServiceUnavailable, "Mailcheck request failed.", innerException: exception);
            }
        }

        throw new MailcheckClientException(MailcheckFailureKind.UnexpectedResponse, "Mailcheck verification failed unexpectedly.");
    }

    private string BuildVerificationUri(string email)
    {
        var template = mailCheckOptions.VerifyEndpointTemplate.Replace("{email}", Uri.EscapeDataString(email.Trim()), StringComparison.Ordinal);
        return $"{mailCheckOptions.BaseUrl.TrimEnd('/')}/{template.TrimStart('/')}";
    }

    private static bool IsMissingDecisionData(MailcheckVerificationResponse response) =>
        response.IsValid is null &&
        response.IsSyntaxValid is null &&
        response.IsDeliverable is null &&
        response.HasMxRecords is null &&
        response.IsDisposable is null &&
        response.IsSpam is null &&
        string.IsNullOrWhiteSpace(response.Risk) &&
        string.IsNullOrWhiteSpace(response.RiskLevel);

    private async Task DelayBeforeRetryAsync(int attempt, TimeSpan? retryAfter, CancellationToken cancellationToken)
    {
        var delay = retryAfter ?? TimeSpan.FromMilliseconds(mailCheckOptions.RetryBaseDelayMilliseconds * attempt);
        await Task.Delay(delay, cancellationToken);
    }

    private static string MaskEmail(string email)
    {
        var normalized = email.Trim();
        var atIndex = normalized.IndexOf('@');
        if (atIndex <= 1)
        {
            return "***";
        }

        var prefix = normalized[..Math.Min(2, atIndex)];
        var domain = atIndex >= 0 ? normalized[atIndex..] : string.Empty;
        return $"{prefix}***{domain}";
    }
}
