using EVotingSystem.Models.Domain;
using EVotingSystem.Options;
using Microsoft.Extensions.Options;

namespace EVotingSystem.Infrastructure.Firestore;

public class FirestoreSeedService(
    ICandidateRepository candidateRepository,
    IElectionStatisticsRepository statisticsRepository,
    IOptions<FirebaseOptions> firebaseOptions,
    IOptions<FirestoreOptions> firestoreOptions,
    IOptions<SeedOptions> seedOptions,
    ILogger<FirestoreSeedService> logger) : IFirestoreSeedService
{
    private readonly FirebaseOptions firebase = firebaseOptions.Value;
    private readonly FirestoreOptions firestore = firestoreOptions.Value;
    private readonly SeedOptions seed = seedOptions.Value;

    public async Task EnsureSeedDataAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!firebase.IsConfigured)
        {
            logger.LogInformation("Skipping Firestore seed because Firebase credentials are not configured.");
            return;
        }

        foreach (var candidate in seed.Candidates)
        {
            var candidateToSeed = Clone(candidate, seed.Election.Id);
            var created = await candidateRepository.CreateIfMissingAsync(candidateToSeed, cancellationToken);
            if (created)
            {
                logger.LogInformation("Seeded candidate {CandidateId} into Firestore.", candidateToSeed.Id);
            }
        }

        var initialStatistics = new PollStatistics
        {
            ElectionId = seed.Election.Id,
            EligibleVoterCount = seed.Election.TotalPopulation,
            ElectionOpen = seed.Election.IsVotingOpen(DateTime.UtcNow),
            GeneratedAtUtc = DateTime.UtcNow,
            VotingRules = seed.Election.VotingRules
        };

        var statsCreated = await statisticsRepository.CreateIfMissingAsync(initialStatistics, cancellationToken);
        if (statsCreated)
        {
            logger.LogInformation(
                "Seeded election statistics document for {ElectionId} in collection {Collection}.",
                seed.Election.Id,
                firestore.Collections.ElectionStats);
        }
    }

    private static Candidate Clone(Candidate candidate, string electionId) =>
        new()
        {
            Id = candidate.Id,
            ElectionId = electionId,
            Name = candidate.Name,
            Party = candidate.Party,
            Slogan = candidate.Slogan,
            Biography = candidate.Biography,
            PhotoUrl = candidate.PhotoUrl,
            VoteCount = candidate.VoteCount,
            IsActive = candidate.IsActive,
            DisplayOrder = candidate.DisplayOrder,
            ProvinceCode = candidate.ProvinceCode,
            ProvinceName = candidate.ProvinceName,
            CreatedAtUtc = candidate.CreatedAtUtc == default ? DateTime.UtcNow : candidate.CreatedAtUtc,
            UpdatedAtUtc = DateTime.UtcNow
        };
}
