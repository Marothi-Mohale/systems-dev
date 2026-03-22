using System.Text.Json;
using EVotingSystem.Models.Domain;
using EVotingSystem.Options;
using Microsoft.Extensions.Options;

namespace EVotingSystem.Infrastructure.Firestore;

public class FirestoreElectionStatisticsRepository(
    IFirestoreDocumentClient client,
    IFirestoreCollectionNameProvider collectionNames,
    IOptions<FirebaseOptions> firebaseOptions,
    ILogger<FirestoreElectionStatisticsRepository> logger) : FirestoreRepositoryBase(client, collectionNames), IElectionStatisticsRepository
{
    private readonly FirebaseOptions firebase = firebaseOptions.Value;

    public async Task<PollStatistics?> GetByElectionIdAsync(string electionId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!firebase.IsConfigured)
        {
            logger.LogInformation("Firebase is not configured; live election stats are unavailable.");
            return null;
        }

        try
        {
            using var document = await Client.GetDocumentAsync($"{Collections.ElectionStats}/{electionId}", cancellationToken);
            return document is null ? null : ParseStatistics(document.RootElement);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Failed reading election statistics for {ElectionId}.", electionId);
            throw new FirestoreException($"Failed reading election statistics for '{electionId}'.", exception);
        }
    }

    public async Task<bool> CreateIfMissingAsync(PollStatistics statistics, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!firebase.IsConfigured)
        {
            return false;
        }

        try
        {
            return await Client.CreateDocumentAsync(Collections.ElectionStats, statistics.ElectionId, BuildDocument(statistics), cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Failed seeding election statistics for {ElectionId}.", statistics.ElectionId);
            throw new FirestoreException($"Failed seeding election statistics for '{statistics.ElectionId}'.", exception);
        }
    }

    public async Task UpsertAsync(PollStatistics statistics, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!firebase.IsConfigured)
        {
            logger.LogDebug("Skipping election stats upsert because Firebase is not configured.");
            return;
        }

        try
        {
            var path = $"{Collections.ElectionStats}/{statistics.ElectionId}";
            var body = BuildDocument(statistics);
            var created = await Client.CreateDocumentAsync(Collections.ElectionStats, statistics.ElectionId, body, cancellationToken);
            if (created)
            {
                return;
            }

            var updatePayload = BuildDocument(statistics, statistics.ElectionId, path);
            await Client.PatchDocumentAsync(
                path,
                updatePayload,
                cancellationToken,
                "totalVotesCast",
                "acceptedVotes",
                "rejectedVotes",
                "eligibleVoterCount",
                "distinctVoterCount",
                "electionOpen",
                "generatedAtUtc",
                "maxVotesPerVoterPerElection",
                "requireAuthenticatedVoter",
                "allowVoteReplacement");
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Failed upserting election statistics for {ElectionId}.", statistics.ElectionId);
            throw new FirestoreException($"Failed upserting election statistics for '{statistics.ElectionId}'.", exception);
        }
    }

    private static PollStatistics ParseStatistics(JsonElement document)
    {
        var fields = GetFields(document);
        return new PollStatistics
        {
            ElectionId = GetDocumentId(document),
            TotalVotesCast = GetInt(fields, "totalVotesCast"),
            AcceptedVotes = GetInt(fields, "acceptedVotes"),
            RejectedVotes = GetInt(fields, "rejectedVotes"),
            EligibleVoterCount = GetInt(fields, "eligibleVoterCount"),
            DistinctVoterCount = GetInt(fields, "distinctVoterCount"),
            ElectionOpen = GetBool(fields, "electionOpen"),
            GeneratedAtUtc = GetNullableDateTime(fields, "generatedAtUtc") ?? DateTime.UtcNow,
            VotingRules = new VotingRules
            {
                MaxVotesPerVoterPerElection = GetInt(fields, "maxVotesPerVoterPerElection") == 0 ? 1 : GetInt(fields, "maxVotesPerVoterPerElection"),
                RequireAuthenticatedVoter = !fields.TryGetProperty("requireAuthenticatedVoter", out var authField) || authField.GetProperty("booleanValue").GetBoolean(),
                AllowVoteReplacement = GetBool(fields, "allowVoteReplacement")
            }
        };
    }

    private static object BuildDocument(PollStatistics statistics, string? documentId = null, string? path = null)
    {
        var values = new Dictionary<string, object?>
        {
            ["electionId"] = documentId ?? statistics.ElectionId,
            ["totalVotesCast"] = statistics.TotalVotesCast,
            ["acceptedVotes"] = statistics.AcceptedVotes,
            ["rejectedVotes"] = statistics.RejectedVotes,
            ["eligibleVoterCount"] = statistics.EligibleVoterCount,
            ["distinctVoterCount"] = statistics.DistinctVoterCount,
            ["electionOpen"] = statistics.ElectionOpen,
            ["generatedAtUtc"] = statistics.GeneratedAtUtc,
            ["maxVotesPerVoterPerElection"] = statistics.VotingRules.MaxVotesPerVoterPerElection,
            ["requireAuthenticatedVoter"] = statistics.VotingRules.RequireAuthenticatedVoter,
            ["allowVoteReplacement"] = statistics.VotingRules.AllowVoteReplacement
        };

        return string.IsNullOrWhiteSpace(path)
            ? BuildDocument(values)
            : BuildDocument(values, path);
    }
}
