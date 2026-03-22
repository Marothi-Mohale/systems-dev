using System.Net;

namespace EVotingSystem.Services;

public class MailcheckClientException : Exception
{
    public MailcheckClientException(MailcheckFailureKind failureKind, string message, HttpStatusCode? statusCode = null, Exception? innerException = null)
        : base(message, innerException)
    {
        FailureKind = failureKind;
        StatusCode = statusCode;
    }

    public MailcheckFailureKind FailureKind { get; }
    public HttpStatusCode? StatusCode { get; }
}

public enum MailcheckFailureKind
{
    NotConfigured,
    RateLimited,
    Timeout,
    MalformedResponse,
    Unauthorized,
    ServiceUnavailable,
    UnexpectedResponse
}
