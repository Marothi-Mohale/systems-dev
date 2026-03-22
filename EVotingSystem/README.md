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
- Mermaid class diagram in [Docs/class-diagram.md](/workspaces/systems-dev/EVotingSystem/Docs/class-diagram.md)

## Local run

```bash
cd /workspaces/systems-dev/EVotingSystem
HOME=/tmp DOTNET_CLI_HOME=/tmp dotnet run
```

If Firebase credentials are not configured, the app uses the in-memory repository so the prototype can still run locally.

## Firebase setup

Add these values in `appsettings.json` or user secrets:

- `Firebase:ProjectId`
- `Firebase:ServiceAccountEmail`
- `Firebase:ServiceAccountPrivateKey`

The private key should be the PEM key from the service account JSON, with newlines preserved or escaped as `\n`.

## Email verification setup

Add your API key:

- `MailCheck:ApiKey`

The app calls `GET https://api.usercheck.com/email/{email}` with `Authorization: Bearer <API_KEY>`.

## Firestore collections

- `elections`
- `candidates`
- `voters`
- `votes`

## Notes

- Election Commission access is intentionally left out, per brief, and simulated through seeded database data.
- Because this workspace has no Firebase credentials yet, Firestore integration is implemented but not exercised in this environment.
