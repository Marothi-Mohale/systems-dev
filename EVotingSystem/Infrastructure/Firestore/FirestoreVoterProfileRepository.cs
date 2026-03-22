using System.Text.Json;
using EVotingSystem.Models.Domain;
using EVotingSystem.Options;
using Microsoft.Extensions.Options;

namespace EVotingSystem.Infrastructure.Firestore;

public class FirestoreVoterProfileRepository(
    IFirestoreDocumentClient client,
    IFirestoreCollectionNameProvider collectionNames,
    IOptions<FirebaseOptions> firebaseOptions,
    ILogger<FirestoreVoterProfileRepository> logger) : FirestoreRepositoryBase(client, collectionNames), IVoterProfileRepository
{
    private readonly FirebaseOptions firebase = firebaseOptions.Value;

    public async Task<VoterProfile?> GetByIdAsync(string voterId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!firebase.IsConfigured)
        {
            return null;
        }

        try
        {
            using var document = await Client.GetDocumentAsync($"{Collections.VoterProfiles}/{voterId}", cancellationToken);
            return document is null ? null : ParseProfile(document.RootElement);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Failed reading voter profile {VoterId}.", voterId);
            throw new FirestoreException($"Failed reading voter profile '{voterId}'.", exception);
        }
    }

    public async Task<bool> CreateIfMissingAsync(VoterProfile profile, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!firebase.IsConfigured)
        {
            return false;
        }

        try
        {
            return await Client.CreateDocumentAsync(Collections.VoterProfiles, profile.Id, BuildDocument(profile), cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Failed seeding voter profile {VoterId}.", profile.Id);
            throw new FirestoreException($"Failed seeding voter profile '{profile.Id}'.", exception);
        }
    }

    private static VoterProfile ParseProfile(JsonElement document)
    {
        var fields = GetFields(document);
        return new VoterProfile
        {
            Id = GetDocumentId(document),
            ApplicationUserId = GetNullableString(fields, "applicationUserId") ?? GetDocumentId(document),
            FullName = GetString(fields, "fullName"),
            Email = GetString(fields, "email"),
            ProvinceCode = GetNullableString(fields, "provinceCode"),
            ProvinceName = GetNullableString(fields, "provinceName"),
            IsEligibleToVote = !fields.TryGetProperty("isEligibleToVote", out var eligibleField) || eligibleField.GetProperty("booleanValue").GetBoolean(),
            HasVoted = GetBool(fields, "hasVoted"),
            SelectedCandidateId = GetNullableString(fields, "selectedCandidateId"),
            RegisteredAtUtc = GetNullableDateTime(fields, "registeredAtUtc") ?? DateTime.UtcNow,
            UpdatedAtUtc = GetNullableDateTime(fields, "updatedAtUtc") ?? DateTime.UtcNow,
            LastLoginAtUtc = GetNullableDateTime(fields, "lastLoginAtUtc"),
            LastVoteAtUtc = GetNullableDateTime(fields, "lastVoteAtUtc")
        };
    }

    private static object BuildDocument(VoterProfile profile) =>
        BuildDocument(new Dictionary<string, object?>
        {
            ["applicationUserId"] = profile.ApplicationUserId,
            ["fullName"] = profile.FullName,
            ["email"] = profile.Email,
            ["provinceCode"] = profile.ProvinceCode,
            ["provinceName"] = profile.ProvinceName,
            ["isEligibleToVote"] = profile.IsEligibleToVote,
            ["hasVoted"] = profile.HasVoted,
            ["selectedCandidateId"] = profile.SelectedCandidateId,
            ["registeredAtUtc"] = profile.RegisteredAtUtc,
            ["updatedAtUtc"] = profile.UpdatedAtUtc,
            ["lastLoginAtUtc"] = profile.LastLoginAtUtc,
            ["lastVoteAtUtc"] = profile.LastVoteAtUtc
        });
}
