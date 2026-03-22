using System.Text.Json;
using EVotingSystem.Models.ViewModels;
using EVotingSystem.Options;
using Microsoft.Extensions.Options;

namespace EVotingSystem.Infrastructure.Firestore;

public class FirestoreVotingTransaction(
    IFirestoreDocumentClient client,
    IFirestoreCollectionNameProvider collectionNames,
    IOptions<FirebaseOptions> firebaseOptions,
    ILogger<FirestoreVotingTransaction> logger) : IFirestoreVotingTransaction
{
    private readonly FirebaseOptions firebase = firebaseOptions.Value;

    public async Task<OperationResult> SubmitVoteAsync(string electionId, string voterId, string candidateId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!firebase.IsConfigured)
        {
            return OperationResult.Failure("Voting persistence is currently unavailable.");
        }

        var voteId = BuildVoteDocumentId(electionId, voterId);
        var voteDocumentPath = $"{collectionNames.Votes}/{voteId}";
        var candidateDocumentPath = $"{collectionNames.Candidates}/{candidateId}";
        var statsDocumentPath = $"{collectionNames.ElectionStats}/{electionId}";
        var castAtUtc = DateTime.UtcNow;

        try
        {
            using var candidateDocument = await client.GetDocumentAsync(candidateDocumentPath, cancellationToken);
            if (candidateDocument is null)
            {
                logger.LogWarning("Vote rejected because candidate {CandidateId} does not exist.", candidateId);
                return OperationResult.Failure("The selected candidate could not be found.");
            }

            using var statsDocument = await client.GetDocumentAsync(statsDocumentPath, cancellationToken);
            if (statsDocument is null)
            {
                logger.LogError("Vote rejected because election stats document {StatsDocumentPath} is missing.", statsDocumentPath);
                return OperationResult.Failure("Voting is temporarily unavailable. Please try again shortly.");
            }

            using var transaction = await client.BeginTransactionAsync(cancellationToken);
            var transactionId = transaction.RootElement.GetProperty("transaction").GetString();
            var voteDocumentName = ToDocumentName(voteDocumentPath);
            var candidateDocumentName = ToDocumentName(candidateDocumentPath);
            var statsDocumentName = ToDocumentName(statsDocumentPath);

            var payload = new
            {
                writes = new object[]
                {
                    new
                    {
                        update = BuildVoteDocument(voteDocumentName, voteId, electionId, voterId, candidateId, castAtUtc),
                        currentDocument = new
                        {
                            exists = false
                        }
                    },
                    new
                    {
                        transform = new
                        {
                            document = candidateDocumentName,
                            fieldTransforms = new object[]
                            {
                                new
                                {
                                    fieldPath = "voteCount",
                                    increment = new
                                    {
                                        integerValue = "1"
                                    }
                                },
                                new
                                {
                                    fieldPath = "updatedAtUtc",
                                    setToServerValue = "REQUEST_TIME"
                                }
                            }
                        }
                    },
                    new
                    {
                        transform = new
                        {
                            document = statsDocumentName,
                            fieldTransforms = new object[]
                            {
                                new
                                {
                                    fieldPath = "totalVotesCast",
                                    increment = new
                                    {
                                        integerValue = "1"
                                    }
                                },
                                new
                                {
                                    fieldPath = "acceptedVotes",
                                    increment = new
                                    {
                                        integerValue = "1"
                                    }
                                },
                                new
                                {
                                    fieldPath = "distinctVoterCount",
                                    increment = new
                                    {
                                        integerValue = "1"
                                    }
                                },
                                new
                                {
                                    fieldPath = "generatedAtUtc",
                                    setToServerValue = "REQUEST_TIME"
                                }
                            }
                        }
                    }
                },
                transaction = transactionId
            };

            await client.CommitAsync(payload, cancellationToken);
            logger.LogInformation(
                "Vote transaction committed successfully for voter {VoterId}, candidate {CandidateId}, election {ElectionId}, vote {VoteId}.",
                voterId,
                candidateId,
                electionId,
                voteId);

            return OperationResult.Success("Your vote has been recorded successfully.");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (FirestoreException exception) when (IsDuplicateVoteFailure(exception))
        {
            logger.LogWarning(
                exception,
                "Duplicate vote blocked for voter {VoterId} in election {ElectionId}.",
                voterId,
                electionId);

            return OperationResult.Failure("You have already submitted your vote. Only one vote is allowed per voter.");
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Vote transaction failed for voter {VoterId}, candidate {CandidateId}, election {ElectionId}.",
                voterId,
                candidateId,
                electionId);

            return OperationResult.Failure("We could not record your vote right now. Please refresh the page and try again.");
        }
    }

    private object BuildVoteDocument(string documentName, string voteId, string electionId, string voterId, string candidateId, DateTime castAtUtc)
    {
        var fields = new Dictionary<string, object?>
        {
            ["id"] = voteId,
            ["electionId"] = electionId,
            ["voterId"] = voterId,
            ["candidateId"] = candidateId,
            ["votingChannel"] = "web",
            ["status"] = "accepted",
            ["idempotencyKey"] = voteId,
            ["castAtUtc"] = castAtUtc,
            ["recordedAtUtc"] = castAtUtc
        };

        return new
        {
            name = documentName,
            fields = fields.ToDictionary(pair => pair.Key, pair => ToFirestoreValue(pair.Value))
        };
    }

    private static object ToFirestoreValue(object? value)
    {
        return value switch
        {
            null => new Dictionary<string, object?> { ["nullValue"] = null },
            string stringValue => new Dictionary<string, object?> { ["stringValue"] = stringValue },
            bool boolValue => new Dictionary<string, object?> { ["booleanValue"] = boolValue },
            int intValue => new Dictionary<string, object?> { ["integerValue"] = intValue.ToString() },
            long longValue => new Dictionary<string, object?> { ["integerValue"] = longValue.ToString() },
            DateTime dateTimeValue => new Dictionary<string, object?> { ["timestampValue"] = dateTimeValue.ToUniversalTime().ToString("O") },
            _ => new Dictionary<string, object?> { ["stringValue"] = value.ToString() ?? string.Empty }
        };
    }

    private string ToDocumentName(string relativePath) =>
        $"projects/{firebase.ProjectId}/databases/{firebase.DatabaseId}/documents/{relativePath}";

    private static string BuildVoteDocumentId(string electionId, string voterId) => $"{electionId}--{voterId}";

    private static bool IsDuplicateVoteFailure(FirestoreException exception) =>
        exception.Message.Contains("FAILED_PRECONDITION", StringComparison.OrdinalIgnoreCase) ||
        exception.Message.Contains("ALREADY_EXISTS", StringComparison.OrdinalIgnoreCase) ||
        exception.Message.Contains("exists = false", StringComparison.OrdinalIgnoreCase);
}
