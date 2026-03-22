using System.Text.Json;

namespace EVotingSystem.Infrastructure.Firestore;

public interface IFirestoreDocumentClient
{
    Task<JsonDocument?> GetDocumentAsync(string path, CancellationToken cancellationToken);
    Task<JsonDocument> ListDocumentsAsync(string collectionId, CancellationToken cancellationToken);
    Task<bool> CreateDocumentAsync(string collectionId, string documentId, object payload, CancellationToken cancellationToken);
    Task PatchDocumentAsync(string path, object payload, CancellationToken cancellationToken, params string[] updateMaskFields);
    Task<JsonDocument> BeginTransactionAsync(CancellationToken cancellationToken);
    Task CommitAsync(object payload, CancellationToken cancellationToken);
}
