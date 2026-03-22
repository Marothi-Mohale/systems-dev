# Election Platform Demo Guide

## Demo candidates

The default demo uses three candidates designed to look good in screenshots and on the ballot:

- Ayanda Dlamini, Ubuntu Forward
- Liam Naidoo, Civic Renewal Movement
- Thandiwe Mokoena, Future Voices Alliance

Their placeholder portraits live in:

- `wwwroot/images/candidates/ayanda-dlamini.svg`
- `wwwroot/images/candidates/liam-naidoo.svg`
- `wwwroot/images/candidates/thandiwe-mokoena.svg`

## Seeding approach

Startup seeding is already built into the application:

- If Firestore is configured and `Firestore:SeedOnStartup=true`, candidates and the election statistics document are created automatically on startup.
- If Firestore is not configured, the application still runs in a demo-safe mode using the `Seed` section from configuration.

## Resetting the environment

### Local demo mode

- Run `./scripts/start-demo-local.sh`
- Restarting the app resets in-memory demo users and returns the app to seeded local data

### Firestore-backed demo mode

- Clear these Firestore collections before the next demo:
  - `candidates`
  - `votes`
  - `electionStats`
  - `voterProfiles`
- Restart the app so startup seeding recreates demo candidates and baseline statistics
- Use `./scripts/reset-demo-notes.sh` for a quick reminder

## Demo user guidance

For marking and demonstration:

- Start as a guest on the Home page
- Open the public Results dashboard
- Register a voter account using a normal, non-disposable email
- Sign in and open the ballot
- Cast one vote
- Return to the public Results page and show the refreshed totals and percentages

Because the prototype uses an in-memory Identity store in local mode:

- demo users do not persist across application restarts
- this is useful for repeating the marking flow cleanly

## Suggested screenshot checklist

- Home page hero and navigation
- Registration page with validation hints
- Login page
- Ballot page with candidate cards
- Vote submitted state / already-voted protection
- Public results dashboard with totals and candidate percentages
- Empty-state example when no live data is available

## Final walkthrough for markers

### Guest experience

- Open the landing page
- Notice the polished navigation, public results access, and clear explanation of the platform
- Open the Results page
- Observe total votes, turnout percentage, per-candidate counts, and percentage bars

### Voter experience

- Register with full name, email, password, and optional province
- Explain that account creation is blocked until email validation succeeds
- Sign in
- Open the ballot
- Select a candidate and submit one vote
- Attempting to vote again shows a friendly one-vote-only message

### Results experience

- Return to the public dashboard
- Highlight that results are visible to guests without login
- Explain that percentages are based on total votes cast and turnout is based on population = 100
- Mention the auto-refresh behavior for near-real-time updates

## Graceful fallback behavior

- If Firestore is temporarily unavailable, the app avoids leaking raw errors to users
- Public results show a safe status notice instead of internals
- Ballot and registration flows surface generic retry guidance rather than backend details
- If Firebase credentials are not configured, the app still demonstrates cleanly with local seeded defaults

## What markers should notice

- clear separation between guest and authenticated voter flows
- validation before account creation
- explicit one-voter-one-vote enforcement
- transaction-based voting consistency design
- polished UI with sensible empty states and mobile-friendly layouts
- resilience: the system remains understandable even when the live database is unavailable
