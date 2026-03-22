using System.Text.Json;
using EVotingSystem.Models.Domain;
using EVotingSystem.Options;
using Microsoft.Extensions.Options;

namespace EVotingSystem.Infrastructure.Firestore;

public class FirestoreCandidateRepository(
    IFirestoreDocumentClient client,
    IFirestoreCollectionNameProvider collectionNames,
    IOptions<FirebaseOptions> firebaseOptions,
    ILogger<FirestoreCandidateRepository> logger) : FirestoreRepositoryBase(client, collectionNames), ICandidateRepository
{
    private readonly FirebaseOptions firebase = firebaseOptions.Value;

    public async Task<IReadOnlyList<Candidate>> GetActiveCandidatesAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!firebase.IsConfigured)
        {
            logger.LogInformation("Firebase is not configured; live candidate reads are unavailable.");
            return [];
        }

        try
        {
            using var document = await Client.ListDocumentsAsync(Collections.Candidates, cancellationToken);
            if (!document.RootElement.TryGetProperty("documents", out var documentsElement))
            {
                return [];
            }

            return documentsElement.EnumerateArray()
                .Select(ParseCandidate)
                .Where(candidate => candidate.IsActive)
                .OrderBy(candidate => candidate.DisplayOrder)
                .ThenBy(candidate => candidate.Name)
                .ToList();
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Failed to read candidates from Firestore collection {Collection}.", Collections.Candidates);
            throw new FirestoreException("Failed to read candidates from Firestore.", exception);
        }
    }

    public async Task<Candidate?> GetByIdAsync(string candidateId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!firebase.IsConfigured)
        {
            logger.LogInformation("Firebase is not configured; candidate lookup for {CandidateId} is unavailable.", candidateId);
            return null;
        }

        try
        {
            using var document = await Client.GetDocumentAsync($"{Collections.Candidates}/{candidateId}", cancellationToken);
            return document is null ? null : ParseCandidate(document.RootElement);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Failed to read candidate {CandidateId} from Firestore.", candidateId);
            throw new FirestoreException($"Failed to read candidate '{candidateId}' from Firestore.", exception);
        }
    }

    public async Task<bool> CreateIfMissingAsync(Candidate candidate, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!firebase.IsConfigured)
        {
            logger.LogDebug("Skipping candidate seeding because Firebase is not configured.");
            return false;
        }

        try
        {
            return await Client.CreateDocumentAsync(Collections.Candidates, candidate.Id, BuildDocument(candidate), cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Failed to seed candidate {CandidateId}.", candidate.Id);
            throw new FirestoreException($"Failed to seed candidate '{candidate.Id}'.", exception);
        }
    }

    private static Candidate ParseCandidate(JsonElement document)
    {
        var fields = GetFields(document);
        return new Candidate
        {
            Id = GetDocumentId(document),
            ElectionId = GetNullableString(fields, "electionId") ?? "default-election",
            Name = GetString(fields, "name"),
            Party = GetString(fields, "party"),
            Slogan = GetString(fields, "slogan"),
            Biography = GetString(fields, "biography"),
            VoteCount = GetInt(fields, "voteCount"),
            IsActive = !fields.TryGetProperty("isActive", out var activeField) || activeField.GetProperty("booleanValue").GetBoolean(),
            DisplayOrder = GetInt(fields, "displayOrder"),
            ProvinceCode = GetNullableString(fields, "provinceCode"),
            ProvinceName = GetNullableString(fields, "provinceName"),
            CreatedAtUtc = GetNullableDateTime(fields, "createdAtUtc") ?? DateTime.UtcNow,
            UpdatedAtUtc = GetNullableDateTime(fields, "updatedAtUtc") ?? DateTime.UtcNow
        };
    }

    private static object BuildDocument(Candidate candidate) =>
        BuildDocument(new Dictionary<string, object?>
        {
            ["electionId"] = candidate.ElectionId,
            ["name"] = candidate.Name,
            ["party"] = candidate.Party,
            ["slogan"] = candidate.Slogan,
            ["biography"] = candidate.Biography,
            ["voteCount"] = candidate.VoteCount,
            ["isActive"] = candidate.IsActive,
            ["displayOrder"] = candidate.DisplayOrder,
            ["provinceCode"] = candidate.ProvinceCode,
            ["provinceName"] = candidate.ProvinceName,
            ["createdAtUtc"] = candidate.CreatedAtUtc,
            ["updatedAtUtc"] = candidate.UpdatedAtUtc
        });
}
