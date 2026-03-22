using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using EVotingSystem.Options;
using Microsoft.Extensions.Options;

namespace EVotingSystem.Services;

public class FirestoreRestClient(IHttpClientFactory httpClientFactory, IOptions<FirebaseOptions> options)
{
    private const string Scope = "https://www.googleapis.com/auth/datastore";
    private string? _accessToken;
    private DateTimeOffset _accessTokenExpiresAt;

    public FirebaseOptions Settings => options.Value;

    public string DocumentRoot =>
        $"https://firestore.googleapis.com/v1/projects/{Settings.ProjectId}/databases/{Settings.DatabaseId}/documents";

    public async Task<JsonDocument?> GetDocumentAsync(string path, CancellationToken cancellationToken)
    {
        using var request = await CreateAuthorizedRequestAsync(HttpMethod.Get, $"{DocumentRoot}/{path}", cancellationToken);
        using var response = await httpClientFactory.CreateClient().SendAsync(request, cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
    }

    public async Task<JsonDocument> ListDocumentsAsync(string collectionId, CancellationToken cancellationToken)
    {
        using var request = await CreateAuthorizedRequestAsync(HttpMethod.Get, $"{DocumentRoot}/{collectionId}", cancellationToken);
        using var response = await httpClientFactory.CreateClient().SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
    }

    public async Task<bool> CreateDocumentAsync(string collectionId, string documentId, object payload, CancellationToken cancellationToken)
    {
        using var request = await CreateAuthorizedRequestAsync(HttpMethod.Post, $"{DocumentRoot}/{collectionId}?documentId={Uri.EscapeDataString(documentId)}", cancellationToken);
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        using var response = await httpClientFactory.CreateClient().SendAsync(request, cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            return false;
        }

        response.EnsureSuccessStatusCode();
        return true;
    }

    public async Task PatchDocumentAsync(string path, object payload, CancellationToken cancellationToken, params string[] updateMaskFields)
    {
        var query = updateMaskFields.Length == 0
            ? string.Empty
            : "?" + string.Join("&", updateMaskFields.Select(field => $"updateMask.fieldPaths={Uri.EscapeDataString(field)}"));

        using var request = await CreateAuthorizedRequestAsync(HttpMethod.Patch, $"{DocumentRoot}/{path}{query}", cancellationToken);
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        using var response = await httpClientFactory.CreateClient().SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task<JsonDocument> BeginTransactionAsync(CancellationToken cancellationToken)
    {
        using var request = await CreateAuthorizedRequestAsync(HttpMethod.Post, $"{DocumentRoot}:beginTransaction", cancellationToken);
        request.Content = new StringContent("{}", Encoding.UTF8, "application/json");
        using var response = await httpClientFactory.CreateClient().SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
    }

    public async Task CommitAsync(object payload, CancellationToken cancellationToken)
    {
        using var request = await CreateAuthorizedRequestAsync(HttpMethod.Post, $"{DocumentRoot}:commit", cancellationToken);
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        using var response = await httpClientFactory.CreateClient().SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
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

        using var response = await httpClientFactory.CreateClient().PostAsync(Settings.TokenUri, content, cancellationToken);
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
