# EVoting System

ASP.NET Core MVC prototype for the INF4027W election platform brief.

## Stack

- Backend: C# with ASP.NET Core MVC
- Authentication: cookie auth with PBKDF2 password hashing
- NoSQL database: Firebase Cloud Firestore via REST API
- Guest view: public live results dashboard
- Voter flow: register, login, vote once

## What is implemented

- Candidate data seeded from configuration into the repository
- Public dashboard with live percentages, total votes, and turnout out of 100
- Registration with disposable email screening through UserCheck/MailCheck-compatible API
- Voter login and single-vote enforcement
- Transaction-oriented Firestore vote recording with atomic candidate vote increments
- Province captured during sign-up for the bonus requirement

## Class diagram

```mermaid
classDiagram
    direction TB

    class IdentityUser {
        <<Framework Base>>
        +string Id
        +string UserName
        +string NormalizedUserName
        +string Email
        +string NormalizedEmail
        +bool EmailConfirmed
        +string PasswordHash
        +string SecurityStamp
        +string ConcurrencyStamp
        +string PhoneNumber
        +bool LockoutEnabled
        +int AccessFailedCount
    }

    class ApplicationUser {
        +string FullName
        +string ProvinceCode
        +bool HasVoted
        +bool MailcheckValidated
        +string MailcheckStatus
        +DateTime RegisteredAtUtc
    }

    class Province {
        +string Code
        +string Name
        +bool IsActive
    }

    class Candidate {
        +string Id
        +string FullName
        +string PartyName
        +string ManifestoSummary
        +string ImageUrl
        +int DisplayOrder
        +int VoteCount
        +bool IsActive
    }

    class Vote {
        +string Id
        +string ElectionId
        +string CandidateId
        +string VoterId
        +DateTime CreatedAtUtc
    }

    class ElectionStats {
        +string ElectionId
        +int TotalVotesCast
        +int PopulationSize
        +decimal TurnoutPercentage
        +DateTime UpdatedAtUtc
    }

    class PollResultRow {
        +string CandidateId
        +string CandidateName
        +string PartyName
        +int VoteCount
        +decimal VotePercentage
    }

    class RegisterRequestModel {
        +string FullName
        +string Email
        +string Password
        +string ConfirmPassword
        +string ProvinceCode
    }

    class VoteSubmissionModel {
        +string CandidateId
    }

    class ResultViewModel {
        +string ElectionTitle
        +int TotalVotes
        +int PopulationSize
        +decimal TurnoutPercentage
        +List~PollResultRow~ Results
    }

    class EmailVerificationResultModel {
        +bool IsAllowed
        +string NormalizedEmail
        +string Reason
        +string RiskLevel
    }

    class BallotViewModel {
        +string VoterName
        +bool AlreadyVoted
        +List~Candidate~ Candidates
    }

    class FirestoreUserDocument {
        +string Id
        +string Email
        +string NormalizedEmail
        +string UserName
        +string NormalizedUserName
        +string PasswordHash
        +string FullName
        +string ProvinceCode
        +bool HasVoted
        +bool MailcheckValidated
        +string MailcheckStatus
        +string SecurityStamp
        +string ConcurrencyStamp
        +DateTime RegisteredAtUtc
    }

    class FirestoreCandidateDocument {
        +string Id
        +string FullName
        +string PartyName
        +string ManifestoSummary
        +string ImageUrl
        +int DisplayOrder
        +int VoteCount
        +bool IsActive
    }

    class FirestoreVoteDocument {
        +string Id
        +string ElectionId
        +string CandidateId
        +string VoterId
        +DateTime CreatedAtUtc
    }

    class FirestoreElectionStatsDocument {
        +string ElectionId
        +int TotalVotesCast
        +int PopulationSize
        +DateTime UpdatedAtUtc
    }

    class IEmailValidationService {
        <<interface>>
        +Task~EmailVerificationResultModel~ ValidateAsync(string email, CancellationToken ct)
    }

    class IResultsService {
        <<interface>>
        +Task~ResultViewModel~ GetPublicResultsAsync(CancellationToken ct)
    }

    class IVotingService {
        <<interface>>
        +Task~BallotViewModel~ GetBallotAsync(string userId, CancellationToken ct)
        +Task~bool~ CastVoteAsync(string userId, VoteSubmissionModel model, CancellationToken ct)
    }

    class IAccountService {
        <<interface>>
        +Task~IdentityResult~ RegisterAsync(RegisterRequestModel model, CancellationToken ct)
        +Task~SignInResult~ LoginAsync(string email, string password, bool rememberMe)
    }

    class ICandidateRepository {
        <<interface>>
        +Task~IReadOnlyList~Candidate~~ GetActiveCandidatesAsync(CancellationToken ct)
        +Task~Candidate?~ GetByIdAsync(string candidateId, CancellationToken ct)
    }

    class IUserRepository {
        <<interface>>
        +Task~ApplicationUser?~ GetByIdAsync(string userId, CancellationToken ct)
        +Task~ApplicationUser?~ GetByEmailAsync(string email, CancellationToken ct)
        +Task UpdateAsync(ApplicationUser user, CancellationToken ct)
    }

    class IVoteRepository {
        <<interface>>
        +Task~bool~ ExistsForVoterAsync(string electionId, string voterId, CancellationToken ct)
        +Task CreateAsync(Vote vote, CancellationToken ct)
    }

    class IElectionStatsRepository {
        <<interface>>
        +Task~ElectionStats~ GetCurrentAsync(CancellationToken ct)
        +Task UpdateAsync(ElectionStats stats, CancellationToken ct)
    }

    class IFirestoreVotingTransaction {
        <<interface>>
        +Task~bool~ CastVoteAtomicallyAsync(string electionId, string voterId, string candidateId, CancellationToken ct)
    }

    class EmailValidationService {
        +Task~EmailVerificationResultModel~ ValidateAsync(string email, CancellationToken ct)
    }

    class ResultsService {
        +Task~ResultViewModel~ GetPublicResultsAsync(CancellationToken ct)
    }

    class VotingService {
        +Task~BallotViewModel~ GetBallotAsync(string userId, CancellationToken ct)
        +Task~bool~ CastVoteAsync(string userId, VoteSubmissionModel model, CancellationToken ct)
    }

    class AccountService {
        +Task~IdentityResult~ RegisterAsync(RegisterRequestModel model, CancellationToken ct)
        +Task~SignInResult~ LoginAsync(string email, string password, bool rememberMe)
    }

    class FirestoreCandidateRepository
    class FirestoreUserRepository
    class FirestoreVoteRepository
    class FirestoreElectionStatsRepository
    class FirestoreVotingTransaction

    IdentityUser <|-- ApplicationUser

    ApplicationUser --> Province : optional
    Vote --> ApplicationUser : cast by
    Vote --> Candidate : for
    ElectionStats o-- PollResultRow : aggregates
    ResultViewModel o-- PollResultRow : contains
    BallotViewModel o-- Candidate : shows

    FirestoreUserDocument ..> ApplicationUser : maps to
    FirestoreCandidateDocument ..> Candidate : maps to
    FirestoreVoteDocument ..> Vote : maps to
    FirestoreElectionStatsDocument ..> ElectionStats : maps to

    EmailValidationService ..|> IEmailValidationService
    ResultsService ..|> IResultsService
    VotingService ..|> IVotingService
    AccountService ..|> IAccountService

    FirestoreCandidateRepository ..|> ICandidateRepository
    FirestoreUserRepository ..|> IUserRepository
    FirestoreVoteRepository ..|> IVoteRepository
    FirestoreElectionStatsRepository ..|> IElectionStatsRepository
    FirestoreVotingTransaction ..|> IFirestoreVotingTransaction

    AccountService --> IEmailValidationService
    AccountService --> IUserRepository
    VotingService --> IUserRepository
    VotingService --> ICandidateRepository
    VotingService --> IVoteRepository
    VotingService --> IElectionStatsRepository
    VotingService --> IFirestoreVotingTransaction
    ResultsService --> ICandidateRepository
    ResultsService --> IElectionStatsRepository
```

## Local run

```bash
cd /workspaces/systems-dev/EVotingSystem
HOME=/tmp DOTNET_CLI_HOME=/tmp dotnet run
```

If Firebase credentials are not configured, the MVC app falls back to the `Seed` configuration values so the prototype can still run locally.

## Firebase setup

Add these values in `appsettings.json` or user secrets:

- `Firebase:ProjectId`
- `Firebase:ServiceAccountEmail`
- `Firebase:ServiceAccountPrivateKey`
- `Firebase:DatabaseId`
- `Firestore:SeedOnStartup`
- `Firestore:Collections:Candidates`
- `Firestore:Collections:Votes`
- `Firestore:Collections:ElectionStats`
- `Firestore:Collections:VoterProfiles`

The private key should be the PEM key from the service account JSON, with newlines preserved or escaped as `\n`.

Example configuration:

```json
{
  "Firebase": {
    "ProjectId": "your-firebase-project-id",
    "DatabaseId": "(default)",
    "ServiceAccountEmail": "firebase-adminsdk@example-project.iam.gserviceaccount.com",
    "ServiceAccountPrivateKey": "-----BEGIN PRIVATE KEY-----\\nYOUR_PRIVATE_KEY_HERE\\n-----END PRIVATE KEY-----\\n",
    "TokenUri": "https://oauth2.googleapis.com/token"
  },
  "Firestore": {
    "SeedOnStartup": true,
    "Collections": {
      "Elections": "elections",
      "Candidates": "candidates",
      "Votes": "votes",
      "ElectionStats": "electionStats",
      "VoterProfiles": "voterProfiles"
    }
  }
}
```

## Email verification setup

Add your API key:

- `MailCheck:ApiKey`

The app calls `GET https://api.usercheck.com/email/{email}` with `Authorization: Bearer <API_KEY>`.

## Firestore collections

- `elections`
- `candidates`
- `votes`
- `electionStats`
- `voterProfiles`

Collection naming is centralized in `Firestore:Collections`, which keeps Firestore-specific naming concerns out of controllers and higher-level MVC services.

## Firestore integration notes

- `Services/FirestoreRestClient.cs` is the low-level async REST client with bearer-token generation, cancellation support, and structured exception logging.
- `Infrastructure/Firestore/FirestoreCandidateRepository.cs` reads candidate data from Firestore, which is what the ballot and public results pages use when Firebase is configured.
- `Infrastructure/Firestore/FirestoreVoteRepository.cs` encapsulates vote writes plus vote existence/count queries.
- `Infrastructure/Firestore/FirestoreElectionStatisticsRepository.cs` encapsulates `electionStats` access.
- `Infrastructure/Firestore/FirestoreSeedService.cs` provides idempotent startup seeding for candidate and stats documents.
- `Infrastructure/Firestore/FirestoreElectionRepository.cs` is the MVC-facing facade that composes the Firestore repositories and falls back to seed configuration only when credentials are intentionally absent.

## Local development vs production

- Local development without Firebase credentials: the MVC app falls back to `Seed` configuration data so pages can still render, and Firestore seeding is skipped.
- Local development with Firebase credentials: prefer `dotnet user-secrets`, environment variables, or your Codespaces secret store instead of checking service-account material into development config files.
- Production deployment: inject `Firebase__ProjectId`, `Firebase__ServiceAccountEmail`, and `Firebase__ServiceAccountPrivateKey` through the host environment or secret manager, and scope the service account to only the required Firestore permissions.
- Production reliability: Firestore failures are logged with collection/document context and surfaced as `FirestoreException`, which keeps Firestore-specific failure handling isolated and testable.

## Notes

- Election Commission access is intentionally left out, per brief, and simulated through seeded database data.
- Because this workspace has no Firebase credentials in this environment, the Firestore integration compiles and is structured for deployment, but runtime database calls were not exercised here.
