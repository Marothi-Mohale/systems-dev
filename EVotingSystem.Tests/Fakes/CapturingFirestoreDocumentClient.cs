using System.Text.Json;
using EVotingSystem.Infrastructure.Firestore;

namespace EVotingSystem.Tests.Fakes;

public sealed class CapturingFirestoreDocumentClient : IFirestoreDocumentClient
{
    public object? LastCommitPayload { get; private set; }
    public JsonDocument? CandidateDocument { get; set; } = JsonDocument.Parse("""{"name":"candidate"}""");
    public JsonDocument? StatsDocument { get; set; } = JsonDocument.Parse("""{"name":"stats"}""");
    public JsonDocument TransactionDocument { get; set; } = JsonDocument.Parse("""{"transaction":"test-transaction"}""");
    public Exception? CommitException { get; set; }

    public Task<JsonDocument?> GetDocumentAsync(string path, CancellationToken cancellationToken)
    {
        if (path.Contains("candidates/", StringComparison.Ordinal))
        {
            return Task.FromResult(CandidateDocument);
        }

        if (path.Contains("electionStats/", StringComparison.Ordinal))
        {
            return Task.FromResult(StatsDocument);
        }

        return Task.FromResult<JsonDocument?>(null);
    }

    public Task<JsonDocument> ListDocumentsAsync(string collectionId, CancellationToken cancellationToken) =>
        Task.FromResult(JsonDocument.Parse("""{"documents":[]}"""));

    public Task<bool> CreateDocumentAsync(string collectionId, string documentId, object payload, CancellationToken cancellationToken) =>
        Task.FromResult(true);

    public Task PatchDocumentAsync(string path, object payload, CancellationToken cancellationToken, params string[] updateMaskFields) =>
        Task.CompletedTask;

    public Task<JsonDocument> BeginTransactionAsync(CancellationToken cancellationToken) =>
        Task.FromResult(TransactionDocument);

    public Task CommitAsync(object payload, CancellationToken cancellationToken)
    {
        LastCommitPayload = payload;
        if (CommitException is not null)
        {
            throw CommitException;
        }

        return Task.CompletedTask;
    }
}
