using EVotingSystem.Models.Domain;
using EVotingSystem.Options;
using Microsoft.Extensions.Options;

namespace EVotingSystem.Services;

public class InMemoryElectionRepository(IOptions<SeedOptions> seedOptions) : IElectionRepository
{
    private readonly Lock _lock = new();
    private bool _seeded;
    private ElectionDefinition _election = new();
    private readonly Dictionary<string, Candidate> _candidates = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, VoterProfile> _voters = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _emailIndex = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<VoteRecord> _votes = [];

    public Task EnsureSeedDataAsync(CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            if (_seeded)
            {
                return Task.CompletedTask;
            }

            _election = seedOptions.Value.Election;
            foreach (var candidate in seedOptions.Value.Candidates)
            {
                candidate.ElectionId = _election.Id;
                _candidates[candidate.Id] = Clone(candidate);
            }

            _seeded = true;
        }

        return Task.CompletedTask;
    }

    public Task<ElectionDefinition> GetElectionAsync(CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            return Task.FromResult(Clone(_election));
        }
    }

    public Task<IReadOnlyList<Candidate>> GetCandidatesAsync(CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            return Task.FromResult<IReadOnlyList<Candidate>>(_candidates.Values.Select(Clone).OrderBy(c => c.Name).ToList());
        }
    }

    public Task<VoterProfile?> GetVoterByEmailAsync(string email, CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            if (_emailIndex.TryGetValue(email, out var voterId) && _voters.TryGetValue(voterId, out var voter))
            {
                return Task.FromResult<VoterProfile?>(Clone(voter));
            }
        }

        return Task.FromResult<VoterProfile?>(null);
    }

    public Task<VoterProfile?> GetVoterByIdAsync(string voterId, CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            return Task.FromResult(_voters.TryGetValue(voterId, out var voter) ? Clone(voter) : null);
        }
    }

    public Task<bool> CreateVoterAsync(VoterProfile voter, CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            if (_emailIndex.ContainsKey(voter.Email))
            {
                return Task.FromResult(false);
            }

            _voters[voter.Id] = Clone(voter);
            _emailIndex[voter.Email] = voter.Id;
            return Task.FromResult(true);
        }
    }

    public Task<bool> UpdateLastLoginAsync(string voterId, DateTime lastLoginUtc, CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            if (!_voters.TryGetValue(voterId, out var voter))
            {
                return Task.FromResult(false);
            }

            voter.LastLoginAtUtc = lastLoginUtc;
            voter.UpdatedAtUtc = DateTime.UtcNow;
            return Task.FromResult(true);
        }
    }

    public Task<bool> CastVoteAsync(string voterId, string candidateId, string electionId, CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            if (!_voters.TryGetValue(voterId, out var voter) || voter.HasVoted || !voter.IsEligibleToVote)
            {
                return Task.FromResult(false);
            }

            if (!_candidates.TryGetValue(candidateId, out var candidate) || !string.Equals(candidate.ElectionId, electionId, StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(false);
            }

            voter.HasVoted = true;
            voter.SelectedCandidateId = candidateId;
            voter.LastVoteAtUtc = DateTime.UtcNow;
            voter.UpdatedAtUtc = DateTime.UtcNow;
            candidate.VoteCount += 1;
            candidate.UpdatedAtUtc = DateTime.UtcNow;
            _votes.Add(new VoteRecord
            {
                Id = Guid.NewGuid().ToString("N"),
                VoterId = voterId,
                CandidateId = candidateId,
                ElectionId = electionId,
                CastAtUtc = DateTime.UtcNow,
                RecordedAtUtc = DateTime.UtcNow
            });

            return Task.FromResult(true);
        }
    }

    private static Candidate Clone(Candidate source) => new()
    {
        Id = source.Id,
        Name = source.Name,
        Party = source.Party,
        Slogan = source.Slogan,
        Biography = source.Biography,
        VoteCount = source.VoteCount,
        ElectionId = source.ElectionId,
        IsActive = source.IsActive,
        DisplayOrder = source.DisplayOrder,
        ProvinceCode = source.ProvinceCode,
        ProvinceName = source.ProvinceName,
        CreatedAtUtc = source.CreatedAtUtc,
        UpdatedAtUtc = source.UpdatedAtUtc
    };

    private static ElectionDefinition Clone(ElectionDefinition source) => new()
    {
        Id = source.Id,
        Title = source.Title,
        Description = source.Description,
        StartsAtUtc = source.StartsAtUtc,
        EndsAtUtc = source.EndsAtUtc,
        TotalPopulation = source.TotalPopulation
    };

    private static VoterProfile Clone(VoterProfile source) => new()
    {
        Id = source.Id,
        ApplicationUserId = source.ApplicationUserId,
        FullName = source.FullName,
        Email = source.Email,
        ProvinceCode = source.ProvinceCode,
        ProvinceName = source.ProvinceName,
        IsEligibleToVote = source.IsEligibleToVote,
        HasVoted = source.HasVoted,
        SelectedCandidateId = source.SelectedCandidateId,
        RegisteredAtUtc = source.RegisteredAtUtc,
        UpdatedAtUtc = source.UpdatedAtUtc,
        LastLoginAtUtc = source.LastLoginAtUtc,
        LastVoteAtUtc = source.LastVoteAtUtc
    };
}
