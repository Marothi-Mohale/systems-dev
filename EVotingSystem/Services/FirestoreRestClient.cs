using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using EVotingSystem.Infrastructure.Firestore;
using EVotingSystem.Options;
using Microsoft.Extensions.Options;

namespace EVotingSystem.Services;

public class FirestoreRestClient(
    IHttpClientFactory httpClientFactory,
    IOptions<FirebaseOptions> options,
    ILogger<FirestoreRestClient> logger) : IFirestoreDocumentClient
{
    private const string Scope = "https://www.googleapis.com/auth/datastore";
    private string? _accessToken;
    private DateTimeOffset _accessTokenExpiresAt;

    public FirebaseOptions Settings => options.Value;

    public string DocumentRoot =>
        $"https://firestore.googleapis.com/v1/projects/{Settings.ProjectId}/databases/{Settings.DatabaseId}/documents";

    public async Task<JsonDocument?> GetDocumentAsync(string path, CancellationToken cancellationToken)
    {
        try
        {
            using var request = await CreateAuthorizedRequestAsync(HttpMethod.Get, $"{DocumentRoot}/{path}", cancellationToken);
            using var response = await httpClientFactory.CreateClient("Firestore").SendAsync(request, cancellationToken);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();
            return JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Firestore GET failed for document path {Path}.", path);
            throw new FirestoreException($"Firestore GET failed for '{path}'.", exception);
        }
    }

    public async Task<JsonDocument> ListDocumentsAsync(string collectionId, CancellationToken cancellationToken)
    {
        try
        {
            using var request = await CreateAuthorizedRequestAsync(HttpMethod.Get, $"{DocumentRoot}/{collectionId}", cancellationToken);
            using var response = await httpClientFactory.CreateClient("Firestore").SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();
            return JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Firestore LIST failed for collection {CollectionId}.", collectionId);
            throw new FirestoreException($"Firestore LIST failed for collection '{collectionId}'.", exception);
        }
    }

    public async Task<bool> CreateDocumentAsync(string collectionId, string documentId, object payload, CancellationToken cancellationToken)
    {
        try
        {
            using var request = await CreateAuthorizedRequestAsync(HttpMethod.Post, $"{DocumentRoot}/{collectionId}?documentId={Uri.EscapeDataString(documentId)}", cancellationToken);
            request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            using var response = await httpClientFactory.CreateClient("Firestore").SendAsync(request, cancellationToken);
            if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                return false;
            }

            response.EnsureSuccessStatusCode();
            return true;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Firestore CREATE failed for {CollectionId}/{DocumentId}.", collectionId, documentId);
            throw new FirestoreException($"Firestore CREATE failed for '{collectionId}/{documentId}'.", exception);
        }
    }

    public async Task PatchDocumentAsync(string path, object payload, CancellationToken cancellationToken, params string[] updateMaskFields)
    {
        var query = updateMaskFields.Length == 0
            ? string.Empty
            : "?" + string.Join("&", updateMaskFields.Select(field => $"updateMask.fieldPaths={Uri.EscapeDataString(field)}"));

        try
        {
            using var request = await CreateAuthorizedRequestAsync(HttpMethod.Patch, $"{DocumentRoot}/{path}{query}", cancellationToken);
            request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            using var response = await httpClientFactory.CreateClient("Firestore").SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Firestore PATCH failed for path {Path}.", path);
            throw new FirestoreException($"Firestore PATCH failed for '{path}'.", exception);
        }
    }

    public async Task<JsonDocument> BeginTransactionAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var request = await CreateAuthorizedRequestAsync(HttpMethod.Post, $"{DocumentRoot}:beginTransaction", cancellationToken);
            request.Content = new StringContent("{}", Encoding.UTF8, "application/json");
            using var response = await httpClientFactory.CreateClient("Firestore").SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();
            return JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Firestore transaction start failed.");
            throw new FirestoreException("Firestore transaction start failed.", exception);
        }
    }

    public async Task CommitAsync(object payload, CancellationToken cancellationToken)
    {
        try
        {
            using var request = await CreateAuthorizedRequestAsync(HttpMethod.Post, $"{DocumentRoot}:commit", cancellationToken);
            request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            using var response = await httpClientFactory.CreateClient("Firestore").SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                logger.LogWarning("Firestore commit failed with status {StatusCode}.", (int)response.StatusCode);
                throw new FirestoreException($"Firestore commit failed with status {(int)response.StatusCode}: {body}");
            }
        }
        catch (FirestoreException)
        {
            throw;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Firestore commit failed.");
            throw new FirestoreException("Firestore commit failed.", exception);
        }
    }

    private async Task<HttpRequestMessage> CreateAuthorizedRequestAsync(HttpMethod method, string url, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await GetAccessTokenAsync(cancellationToken));
        return request;
    }

    private async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(_accessToken) && _accessTokenExpiresAt > DateTimeOffset.UtcNow.AddMinutes(2))
        {
            return _accessToken;
        }

        var assertion = CreateJwtAssertion();
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "urn:ietf:params:oauth:grant-type:jwt-bearer",
            ["assertion"] = assertion
        });

        using var response = await httpClientFactory.CreateClient("Firestore").PostAsync(Settings.TokenUri, content, cancellationToken);
        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        _accessToken = document.RootElement.GetProperty("access_token").GetString();
        var expiresInSeconds = document.RootElement.GetProperty("expires_in").GetInt32();
        _accessTokenExpiresAt = DateTimeOffset.UtcNow.AddSeconds(expiresInSeconds);

        return _accessToken!;
    }

    private string CreateJwtAssertion()
    {
        var now = DateTimeOffset.UtcNow;
        var payload = new Dictionary<string, object>
        {
            ["iss"] = Settings.ServiceAccountEmail,
            ["scope"] = Scope,
            ["aud"] = Settings.TokenUri,
            ["iat"] = now.ToUnixTimeSeconds(),
            ["exp"] = now.AddMinutes(55).ToUnixTimeSeconds()
        };

        var headerJson = JsonSerializer.Serialize(new Dictionary<string, object>
        {
            ["alg"] = "RS256",
            ["typ"] = "JWT"
        });
        var payloadJson = JsonSerializer.Serialize(payload);

        var encodedHeader = Base64UrlEncode(Encoding.UTF8.GetBytes(headerJson));
        var encodedPayload = Base64UrlEncode(Encoding.UTF8.GetBytes(payloadJson));
        var unsignedToken = $"{encodedHeader}.{encodedPayload}";

        using var rsa = RSA.Create();
        rsa.ImportFromPem(Settings.ServiceAccountPrivateKey.Replace("\\n", "\n"));
        var signature = rsa.SignData(Encoding.UTF8.GetBytes(unsignedToken), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        return $"{unsignedToken}.{Base64UrlEncode(signature)}";
    }

    private static string Base64UrlEncode(byte[] input) =>
        Convert.ToBase64String(input).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
