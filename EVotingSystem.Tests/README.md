# EVotingSystem Tests

## Structure

- `Controllers/`
  - controller-level guardrail tests for registration flow behavior
- `Infrastructure/`
  - Firestore transaction tests focused on atomic vote persistence strategy
- `Services/`
  - unit tests for email validation, vote-once service logic, and result calculations
- `Fakes/`
  - hand-written test doubles for Firestore, Mailcheck, logging, and Identity dependencies
- `Testing/`
  - lightweight assertion helpers and test-case runner infrastructure

## Why hand-written fakes

The project uses service abstractions cleanly, so small fakes are enough to keep the tests readable without bringing in a mocking framework. This keeps the suite maintainable and makes intent obvious in each test.

## Highest-priority automated coverage

- valid email required before registration
- user cannot vote twice
- result percentages are calculated correctly
- Firestore vote transaction uses atomic increment transforms and a vote-once precondition

## Concurrency test strategy

The most important concurrency guarantee is implemented in Firestore, so automated coverage should happen at two levels:

1. Unit-level verification
   - assert that the transaction writes a deterministic vote document ID
   - assert that the vote document uses `currentDocument.exists = false`
   - assert that candidate and stats documents use Firestore atomic `increment` transforms

2. Environment/integration verification
   - against a Firebase emulator or non-production Firestore project, submit simultaneous votes from two different users to the same candidate
   - start from a known count such as 9 and verify the final count is 11
   - submit simultaneous duplicate votes from the same user and verify exactly one succeeds

## Manual testing still needed

- end-to-end browser flow for register -> login -> ballot -> results refresh
- anti-forgery behavior with real forms and cookies
- rate limiting behavior under repeated failed login / repeated vote-submit attempts
- public results polling behavior when the backend is temporarily unavailable
- Firebase credential loading through environment variables or user-secrets
- production-like concurrency against real Firestore or the Firebase emulator
