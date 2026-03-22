using System.Text.Json;
using System.Text.Json.Nodes;
using EVotingSystem.Models.Domain;
using EVotingSystem.Options;
using Microsoft.Extensions.Options;

namespace EVotingSystem.Services;

public class FirestoreElectionRepository(FirestoreRestClient firestore, IOptions<SeedOptions> seedOptions) : IElectionRepository
{
    private readonly SeedOptions _seed = seedOptions.Value;

    public async Task EnsureSeedDataAsync(CancellationToken cancellationToken)
    {
        var election = await firestore.GetDocumentAsync($"elections/{_seed.Election.Id}", cancellationToken);
        if (election is null)
        {
            await firestore.CreateDocumentAsync("elections", _seed.Election.Id, BuildDocument(_seed.Election), cancellationToken);
        }

        foreach (var candidate in _seed.Candidates)
        {
            var existing = await firestore.GetDocumentAsync($"candidates/{candidate.Id}", cancellationToken);
            if (existing is null)
            {
                await firestore.CreateDocumentAsync("candidates", candidate.Id, BuildDocument(candidate), cancellationToken);
            }
        }
    }

    public async Task<ElectionDefinition> GetElectionAsync(CancellationToken cancellationToken)
    {
        var document = await firestore.GetDocumentAsync($"elections/{_seed.Election.Id}", cancellationToken);
        return document is null ? _seed.Election : ParseElection(document.RootElement);
    }

    public async Task<IReadOnlyList<Candidate>> GetCandidatesAsync(CancellationToken cancellationToken)
    {
        using var document = await firestore.ListDocumentsAsync("candidates", cancellationToken);
        if (!document.RootElement.TryGetProperty("documents", out var documentsElement))
        {
            return [];
        }

        return documentsElement.EnumerateArray()
            .Select(ParseCandidate)
            .OrderBy(candidate => candidate.Name)
            .ToList();
    }

    public async Task<VoterProfile?> GetVoterByEmailAsync(string email, CancellationToken cancellationToken)
    {
        var sanitizedId = ToDocumentId(email);
        var document = await firestore.GetDocumentAsync($"voters/{sanitizedId}", cancellationToken);
        return document is null ? null : ParseVoter(document.RootElement);
    }

    public async Task<VoterProfile?> GetVoterByIdAsync(string voterId, CancellationToken cancellationToken)
    {
        var document = await firestore.GetDocumentAsync($"voters/{voterId}", cancellationToken);
        return document is null ? null : ParseVoter(document.RootElement);
    }

    public async Task<bool> CreateVoterAsync(VoterProfile voter, CancellationToken cancellationToken)
    {
        return await firestore.CreateDocumentAsync("voters", voter.Id, BuildDocument(voter), cancellationToken);
    }

    public async Task<bool> UpdateLastLoginAsync(string voterId, DateTime lastLoginUtc, CancellationToken cancellationToken)
    {
        var document = await firestore.GetDocumentAsync($"voters/{voterId}", cancellationToken);
        if (document is null)
        {
            return false;
        }

        var payload = BuildDocument(new Dictionary<string, object?>
        {
            ["lastLoginUtc"] = lastLoginUtc
        }, GetDocumentName($"voters/{voterId}"));

        await firestore.PatchDocumentAsync($"voters/{voterId}", payload, cancellationToken, "lastLoginUtc");
        return true;
    }

    public async Task<bool> CastVoteAsync(string voterId, string candidateId, string electionId, CancellationToken cancellationToken)
    {
        using var voterDocument = await firestore.GetDocumentAsync($"voters/{voterId}", cancellationToken);
        using var candidateDocument = await firestore.GetDocumentAsync($"candidates/{candidateId}", cancellationToken);

        if (voterDocument is null || candidateDocument is null)
        {
            return false;
        }

        var voter = ParseVoter(voterDocument.RootElement);
        if (voter.HasVoted)
        {
            return false;
        }

        using var transaction = await firestore.BeginTransactionAsync(cancellationToken);
        var transactionId = transaction.RootElement.GetProperty("transaction").GetString();
        var voterUpdateTime = voterDocument.RootElement.GetProperty("updateTime").GetString();
        var candidateName = candidateDocument.RootElement.GetProperty("name").GetString();
        var voteId = Guid.NewGuid().ToString("N");

        var payload = new
        {
            writes = new object[]
            {
                new
                {
                    update = BuildDocument(new Dictionary<string, object?>
                    {
                        ["applicationUserId"] = voter.ApplicationUserId,
                        ["fullName"] = voter.FullName,
                        ["email"] = voter.Email,
                        ["provinceCode"] = voter.ProvinceCode,
                        ["provinceName"] = voter.ProvinceName,
                        ["isEligibleToVote"] = voter.IsEligibleToVote,
                        ["hasVoted"] = true,
                        ["selectedCandidateId"] = candidateId,
                        ["registeredAtUtc"] = voter.RegisteredAtUtc,
                        ["updatedAtUtc"] = DateTime.UtcNow,
                        ["lastLoginAtUtc"] = voter.LastLoginAtUtc,
                        ["lastVoteAtUtc"] = DateTime.UtcNow
                    }, GetDocumentName($"voters/{voterId}")),
                    currentDocument = new
                    {
                        updateTime = voterUpdateTime
                    }
                },
                new
                {
                    transform = new
                    {
                        document = candidateName,
                        fieldTransforms = new object[]
                        {
                            new
                            {
                                fieldPath = "voteCount",
                                increment = new
                                {
                                    integerValue = "1"
                                }
                            }
                        }
                    }
                },
                new
                {
                    update = BuildDocument(new Dictionary<string, object?>
                    {
                        ["voterId"] = voterId,
                        ["candidateId"] = candidateId,
                        ["electionId"] = electionId,
                        ["status"] = "accepted",
                        ["votingChannel"] = "web",
                        ["castAtUtc"] = DateTime.UtcNow,
                        ["recordedAtUtc"] = DateTime.UtcNow
                    }, GetDocumentName($"votes/{voteId}"))
                }
            },
            transaction = transactionId
        };

        await firestore.CommitAsync(payload, cancellationToken);
        return true;
    }

    private static Candidate ParseCandidate(JsonElement document)
    {
        var fields = document.GetProperty("fields");
        return new Candidate
        {
            Id = GetDocumentId(document),
            ElectionId = GetNullableString(fields, "electionId") ?? "default-election",
            Name = GetString(fields, "name"),
            Party = GetString(fields, "party"),
            Slogan = GetString(fields, "slogan"),
            Biography = GetString(fields, "biography"),
            VoteCount = GetInt(fields, "voteCount"),
            IsActive = !fields.TryGetProperty("isActive", out var isActiveElement) || isActiveElement.GetProperty("booleanValue").GetBoolean(),
            DisplayOrder = GetInt(fields, "displayOrder"),
            ProvinceCode = GetNullableString(fields, "provinceCode"),
            ProvinceName = GetNullableString(fields, "provinceName"),
            CreatedAtUtc = GetNullableDateTime(fields, "createdAtUtc") ?? DateTime.UtcNow,
            UpdatedAtUtc = GetNullableDateTime(fields, "updatedAtUtc") ?? DateTime.UtcNow
        };
    }

    private static ElectionDefinition ParseElection(JsonElement document)
    {
        var fields = document.GetProperty("fields");
        return new ElectionDefinition
        {
            Id = GetDocumentId(document),
            Title = GetString(fields, "title"),
            Description = GetString(fields, "description"),
            StartsAtUtc = GetDateTime(fields, "startsAtUtc"),
            EndsAtUtc = GetDateTime(fields, "endsAtUtc"),
            TotalPopulation = GetInt(fields, "totalPopulation")
        };
    }

    private static VoterProfile ParseVoter(JsonElement document)
    {
        var fields = document.GetProperty("fields");
        return new VoterProfile
        {
            Id = GetDocumentId(document),
            ApplicationUserId = GetNullableString(fields, "applicationUserId") ?? GetDocumentId(document),
            FullName = GetString(fields, "fullName"),
            Email = GetString(fields, "email"),
            ProvinceCode = GetNullableString(fields, "provinceCode"),
            ProvinceName = GetNullableString(fields, "provinceName"),
            IsEligibleToVote = !fields.TryGetProperty("isEligibleToVote", out var eligibleElement) || eligibleElement.GetProperty("booleanValue").GetBoolean(),
            HasVoted = GetBool(fields, "hasVoted"),
            SelectedCandidateId = GetNullableString(fields, "selectedCandidateId"),
            RegisteredAtUtc = GetNullableDateTime(fields, "registeredAtUtc") ?? GetDateTime(fields, "createdAtUtc"),
            UpdatedAtUtc = GetNullableDateTime(fields, "updatedAtUtc") ?? DateTime.UtcNow,
            LastLoginAtUtc = GetNullableDateTime(fields, "lastLoginAtUtc") ?? GetNullableDateTime(fields, "lastLoginUtc"),
            LastVoteAtUtc = GetNullableDateTime(fields, "lastVoteAtUtc")
        };
    }

    private object BuildDocument(ElectionDefinition election) =>
        BuildDocument(new Dictionary<string, object?>
        {
            ["title"] = election.Title,
            ["description"] = election.Description,
            ["startsAtUtc"] = election.StartsAtUtc,
            ["endsAtUtc"] = election.EndsAtUtc,
            ["totalPopulation"] = election.TotalPopulation
        });

    private object BuildDocument(Candidate candidate) =>
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

    private object BuildDocument(VoterProfile voter) =>
        BuildDocument(new Dictionary<string, object?>
        {
            ["applicationUserId"] = voter.ApplicationUserId,
            ["fullName"] = voter.FullName,
            ["email"] = voter.Email,
            ["provinceCode"] = voter.ProvinceCode,
            ["provinceName"] = voter.ProvinceName,
            ["isEligibleToVote"] = voter.IsEligibleToVote,
            ["hasVoted"] = voter.HasVoted,
            ["selectedCandidateId"] = voter.SelectedCandidateId,
            ["registeredAtUtc"] = voter.RegisteredAtUtc,
            ["updatedAtUtc"] = voter.UpdatedAtUtc,
            ["lastLoginAtUtc"] = voter.LastLoginAtUtc,
            ["lastVoteAtUtc"] = voter.LastVoteAtUtc
        });

    private object BuildDocument(Dictionary<string, object?> values, string? name = null)
    {
        var fields = new JsonObject();
        foreach (var pair in values)
        {
            fields[pair.Key] = ToFirestoreValue(pair.Value);
        }

        var document = new JsonObject
        {
            ["fields"] = fields
        };

        if (!string.IsNullOrWhiteSpace(name))
        {
            document["name"] = name;
        }

        return document;
    }

    private static JsonObject ToFirestoreValue(object? value)
    {
        return value switch
        {
            null => new JsonObject { ["nullValue"] = null },
            string stringValue => new JsonObject { ["stringValue"] = stringValue },
            bool boolValue => new JsonObject { ["booleanValue"] = boolValue },
            int intValue => new JsonObject { ["integerValue"] = intValue.ToString() },
            long longValue => new JsonObject { ["integerValue"] = longValue.ToString() },
            DateTime dateTimeValue => new JsonObject { ["timestampValue"] = dateTimeValue.ToUniversalTime().ToString("O") },
            _ => new JsonObject { ["stringValue"] = value.ToString() }
        };
    }

    private static string GetDocumentId(JsonElement document) => document.GetProperty("name").GetString()!.Split('/').Last();
    private static string GetString(JsonElement fields, string name) => fields.GetProperty(name).GetProperty("stringValue").GetString() ?? string.Empty;
    private static string? GetNullableString(JsonElement fields, string name) => fields.TryGetProperty(name, out var element) && element.TryGetProperty("stringValue", out var value) ? value.GetString() : null;
    private static bool GetBool(JsonElement fields, string name) => fields.TryGetProperty(name, out var element) && element.GetProperty("booleanValue").GetBoolean();
    private static int GetInt(JsonElement fields, string name) => fields.TryGetProperty(name, out var element) ? int.Parse(element.GetProperty("integerValue").GetString() ?? "0") : 0;
    private static DateTime GetDateTime(JsonElement fields, string name) => DateTime.Parse(fields.GetProperty(name).GetProperty("timestampValue").GetString() ?? DateTime.UtcNow.ToString("O"));
    private static DateTime? GetNullableDateTime(JsonElement fields, string name) => fields.TryGetProperty(name, out var element) && element.TryGetProperty("timestampValue", out var value)
        ? DateTime.Parse(value.GetString() ?? DateTime.UtcNow.ToString("O"))
        : null;

    private static string ToDocumentId(string email) => email.Trim().ToLowerInvariant().Replace("@", "_at_").Replace(".", "_");
    private string GetDocumentName(string relativePath) => $"projects/{firestore.Settings.ProjectId}/databases/{firestore.Settings.DatabaseId}/documents/{relativePath}";
}
