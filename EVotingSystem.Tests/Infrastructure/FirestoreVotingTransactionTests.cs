using System.Text.Json;
using EVotingSystem.Infrastructure.Firestore;
using EVotingSystem.Options;
using EVotingSystem.Tests.Fakes;
using EVotingSystem.Tests.Testing;
using Microsoft.Extensions.Options;

namespace EVotingSystem.Tests.Infrastructure;

public static class FirestoreVotingTransactionTests
{
    public static IEnumerable<TestCase> GetCases()
    {
        yield return new TestCase(
            "Firestore voting transaction uses atomic increments and vote-once precondition",
            UsesAtomicIncrementAndVoteCreatePreconditionAsync);
        yield return new TestCase(
            "Firestore voting transaction converts duplicate commit failures into friendly duplicate-vote result",
            ConvertsDuplicateCommitFailureAsync);
    }

    private static async Task UsesAtomicIncrementAndVoteCreatePreconditionAsync()
    {
        var client = new CapturingFirestoreDocumentClient();
        var transaction = CreateTransaction(client);

        var result = await transaction.SubmitVoteAsync("2026-national-election", "voter-1", "candidate-1", CancellationToken.None);
        AssertEx.True(result.Succeeded);
        AssertEx.NotNull(client.LastCommitPayload);

        using var payload = JsonDocument.Parse(JsonSerializer.Serialize(client.LastCommitPayload));
        var writes = payload.RootElement.GetProperty("writes");

        AssertEx.Equal(3, writes.GetArrayLength());
        AssertEx.False(writes[0].GetProperty("currentDocument").GetProperty("exists").GetBoolean());
        AssertEx.Equal("1", writes[1].GetProperty("transform").GetProperty("fieldTransforms")[0].GetProperty("increment").GetProperty("integerValue").GetString()!);
        AssertEx.Equal("1", writes[2].GetProperty("transform").GetProperty("fieldTransforms")[0].GetProperty("increment").GetProperty("integerValue").GetString()!);
    }

    private static async Task ConvertsDuplicateCommitFailureAsync()
    {
        var client = new CapturingFirestoreDocumentClient
        {
            CommitException = new FirestoreException("ALREADY_EXISTS")
        };

        var transaction = CreateTransaction(client);
        var result = await transaction.SubmitVoteAsync("2026-national-election", "voter-1", "candidate-1", CancellationToken.None);

        AssertEx.False(result.Succeeded);
        AssertEx.Contains("already submitted", result.Message);
    }

    private static FirestoreVotingTransaction CreateTransaction(CapturingFirestoreDocumentClient client) =>
        new(
            client,
            new FakeCollectionNameProvider(),
            Microsoft.Extensions.Options.Options.Create(new FirebaseOptions
            {
                ProjectId = "real-project",
                DatabaseId = "(default)",
                ServiceAccountEmail = "service@example.org",
                ServiceAccountPrivateKey = "not-a-placeholder"
            }),
            new TestLogger<FirestoreVotingTransaction>());
}
