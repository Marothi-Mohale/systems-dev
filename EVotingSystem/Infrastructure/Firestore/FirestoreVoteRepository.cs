using System.Text.Json;
using EVotingSystem.Models.Domain;
using EVotingSystem.Options;
using Microsoft.Extensions.Options;

namespace EVotingSystem.Infrastructure.Firestore;

public class FirestoreVoteRepository(
    IFirestoreDocumentClient client,
    IFirestoreCollectionNameProvider collectionNames,
    IOptions<FirebaseOptions> firebaseOptions,
    ILogger<FirestoreVoteRepository> logger) : FirestoreRepositoryBase(client, collectionNames), IVoteRepository
{
    private readonly FirebaseOptions firebase = firebaseOptions.Value;

    public async Task<bool> ExistsForVoterAsync(string electionId, string voterId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!firebase.IsConfigured)
        {
            return false;
        }

        try
        {
            var voteId = BuildVoteDocumentId(electionId, voterId);
            using var document = await Client.GetDocumentAsync($"{Collections.Votes}/{voteId}", cancellationToken);
            return document is not null;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Failed checking vote existence for voter {VoterId}.", voterId);
            throw new FirestoreException($"Failed checking votes for voter '{voterId}'.", exception);
        }
    }

    public async Task CreateAsync(VoteRecord vote, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!firebase.IsConfigured)
        {
            logger.LogDebug("Skipping vote persistence because Firebase is not configured.");
            return;
        }

        try
        {
            await Client.CreateDocumentAsync(Collections.Votes, vote.Id, BuildDocument(vote), cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Failed to create vote record {VoteId}.", vote.Id);
            throw new FirestoreException($"Failed creating vote '{vote.Id}'.", exception);
        }
    }

    public async Task<int> CountAcceptedVotesAsync(string electionId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!firebase.IsConfigured)
        {
            return 0;
        }

        try
        {
            using var document = await Client.ListDocumentsAsync(Collections.Votes, cancellationToken);
            if (!document.RootElement.TryGetProperty("documents", out var documentsElement))
            {
                return 0;
            }

            return documentsElement.EnumerateArray()
                .Select(ParseVoteRecord)
                .Count(vote => string.Equals(vote.ElectionId, electionId, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(vote.Status, "accepted", StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Failed counting accepted votes for election {ElectionId}.", electionId);
            throw new FirestoreException($"Failed counting accepted votes for election '{electionId}'.", exception);
        }
    }

    private static VoteRecord ParseVoteRecord(JsonElement document)
    {
        var fields = GetFields(document);
        return new VoteRecord
        {
            Id = GetDocumentId(document),
            ElectionId = GetNullableString(fields, "electionId") ?? "default-election",
            VoterId = GetString(fields, "voterId"),
            CandidateId = GetString(fields, "candidateId"),
            VotingChannel = GetNullableString(fields, "votingChannel") ?? "web",
            Status = GetNullableString(fields, "status") ?? "accepted",
            RejectionReason = GetNullableString(fields, "rejectionReason"),
            IdempotencyKey = GetNullableString(fields, "idempotencyKey"),
            CastAtUtc = GetDateTime(fields, "castAtUtc"),
            RecordedAtUtc = GetNullableDateTime(fields, "recordedAtUtc") ?? DateTime.UtcNow
        };
    }

    private static object BuildDocument(VoteRecord vote) =>
        BuildDocument(new Dictionary<string, object?>
        {
            ["electionId"] = vote.ElectionId,
            ["voterId"] = vote.VoterId,
            ["candidateId"] = vote.CandidateId,
            ["votingChannel"] = vote.VotingChannel,
            ["status"] = vote.Status,
            ["rejectionReason"] = vote.RejectionReason,
            ["idempotencyKey"] = vote.IdempotencyKey,
            ["castAtUtc"] = vote.CastAtUtc,
            ["recordedAtUtc"] = vote.RecordedAtUtc
        });

    private static string BuildVoteDocumentId(string electionId, string voterId) => $"{electionId}--{voterId}";
}
