# Election Platform

Election Platform is an ASP.NET Core MVC web application that demonstrates a secure, modern voting workflow for an academic election-system brief. It combines a polished public-facing experience with authenticated voting, transactional Firestore persistence, email-quality screening, and production-conscious deployment practices.

The project is designed to speak to multiple audiences at once:

- lecturers evaluating alignment to an academic brief
- recruiters reviewing engineering quality and system thinking
- senior developers looking for architecture, security, and maintainability signals

## Overview

The platform supports two primary user journeys:

- a guest journey for viewing public election information and live results
- an authenticated voter journey for registration, sign-in, ballot access, and single-vote submission

The application uses ASP.NET Core MVC for presentation and request orchestration, Firebase Cloud Firestore for cloud-native persistence, and Mailcheck.ai for risk-aware email validation during registration. It also includes local/demo-safe behavior so the project can be explored without a full cloud deployment.

## Features

- Public landing page and results dashboard
- Voter registration and sign-in
- Email screening via Mailcheck.ai before account creation is accepted
- One-person, one-vote enforcement
- Firestore-backed vote persistence
- Transaction-safe vote recording and tally updates
- Configurable startup seeding for demo or staged environments
- Secure cookies, antiforgery validation, rate limiting, HSTS, and proxy-aware hosting behavior
- Production-aware static asset caching and response compression
- Automated test coverage for controllers, services, and Firestore-related workflows

## Tech Stack

- Backend: C# / ASP.NET Core MVC
- UI rendering: Razor Views
- Authentication: ASP.NET Core Identity with cookie authentication
- Data store: Firebase Cloud Firestore via REST API
- Email validation: Mailcheck.ai
- Testing: xUnit-style .NET test project under `EVotingSystem.Tests`
- Deployment target: standard ASP.NET hosting, IIS, App Service, Linux reverse-proxy hosting, or cloud platforms

## Why Firestore Was Used

Firestore was selected because it suits the shape of the problem well:

- election, candidate, voter-profile, and vote data map naturally to document collections
- cloud-hosted availability and managed infrastructure reduce operational overhead
- the platform benefits from flexible schema evolution during an academic prototype phase
- transactional write support enables safe vote recording and aggregate updates
- REST-based integration allows explicit control over request handling without introducing a heavyweight ORM or relational schema for a prototype

From an engineering perspective, Firestore also helps demonstrate cloud integration, configuration management, transactional thinking, and repository abstraction in a way that is easy to reason about during assessment and review.

## System Architecture Summary

At a high level, the platform follows a layered web-application architecture:

1. Presentation layer
   ASP.NET Core MVC controllers and Razor Views handle HTTP requests, validation feedback, and page rendering.
2. Application/service layer
   Services coordinate use cases such as registration checks, results calculation, and vote submission.
3. Domain and view-model layer
   Domain models represent election concepts, while view models shape data for the UI.
4. Infrastructure layer
   Firestore repositories, a Firestore REST client, Mailcheck integration, and Identity storage handle external systems and persistence.

This separation keeps the core voting flow understandable, testable, and adaptable for future enhancements.

## OOP and MVC Design Summary

The project uses object-oriented design to model the problem space with clear responsibilities:

- controllers focus on web concerns and user flow
- services encapsulate business logic
- interfaces define contracts for repositories and integrations
- infrastructure classes implement those contracts for Firestore and Mailcheck
- domain models represent election entities such as candidates, voters, votes, and statistics

The MVC pattern is applied deliberately:

- Models: domain entities, options classes, DTOs, and view models
- Views: Razor pages for home, registration, login, ballot, and results
- Controllers: request orchestration for account, election, results, and home flows

This design supports maintainability, testability, and strong separation of concerns.

## Authentication Approach

Authentication is implemented with ASP.NET Core Identity and application cookies:

- users register with a full name, email address, password, and optional province information
- passwords are handled through the Identity pipeline and stored securely as hashes
- sign-in state is maintained with secure, HTTP-only cookies
- antiforgery protection is applied globally to unsafe form submissions
- login and voting endpoints are rate-limited to reduce abuse risk

The design deliberately separates authentication from voting logic so that voter identity, authorization, and ballot rules remain clear and extensible.

## Mailcheck.ai Integration Summary

Mailcheck.ai is used during registration to improve input quality and reduce low-trust sign-ups. Before a new account is accepted, the system checks the supplied email address against Mailcheck policy rules such as:

- syntax validity
- deliverability
- MX record presence
- disposable email detection
- spam or risky classification

The integration is wrapped behind service abstractions so the registration flow stays testable and resilient. Errors are handled safely, and the user sees a controlled message rather than raw external API failures.

## Transaction-Safe Voting Explanation

Voting is the highest-integrity path in the system, so it is designed around transactional consistency. When a voter submits a ballot, the application coordinates a Firestore transaction-like commit flow that:

- verifies the voter and candidate state
- marks the voter as having voted
- records the vote itself
- increments candidate totals atomically
- updates election statistics consistently

This matters because vote integrity is not just about storing a choice. It is also about preventing duplicate votes, avoiding partial updates, and ensuring that public results reflect a consistent state. The current implementation demonstrates the right architectural concern for atomic operations in election software.

## Setup Instructions

### Prerequisites

- .NET SDK 10.0 or later
- a Firebase project with Firestore enabled
- a Mailcheck.ai API key for full registration validation

### Clone and restore

```bash
git clone <your-repository-url>
cd /workspaces/systems-dev
dotnet restore ElectionPlatform.slnx
```

## Firebase Setup

Create or select a Firebase / Google Cloud project, enable Firestore, and provision a service account with the minimum Firestore permissions required for this application.

Configure these values through user secrets, environment variables, or host configuration:

- `Firebase__ProjectId`
- `Firebase__DatabaseId`
- `Firebase__ServiceAccountEmail`
- `Firebase__ServiceAccountPrivateKey`
- `Firebase__TokenUri`
- `Firestore__SeedOnStartup`
- `Firestore__Collections__Elections`
- `Firestore__Collections__Candidates`
- `Firestore__Collections__Votes`
- `Firestore__Collections__ElectionStats`
- `Firestore__Collections__VoterProfiles`

Example:

```json
{
  "Firebase": {
    "ProjectId": "your-gcp-project-id",
    "DatabaseId": "(default)",
    "ServiceAccountEmail": "firebase-adminsdk@example-project.iam.gserviceaccount.com",
    "ServiceAccountPrivateKey": "-----BEGIN PRIVATE KEY-----\\nYOUR_PRIVATE_KEY\\n-----END PRIVATE KEY-----\\n",
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

Note:

- preserve private-key newlines exactly, or store them as escaped `\n` characters when required by the host
- for production, keep `Firestore__SeedOnStartup=false` unless you are intentionally deploying a demo environment

## Mailcheck Setup

Add the Mailcheck API key and related options through configuration:

- `MailCheck__ApiKey`
- `MailCheck__BaseUrl`
- `MailCheck__VerifyEndpointTemplate`
- `MailCheck__TimeoutSeconds`
- `MailCheck__MaxAttempts`
- `MailCheck__RetryBaseDelayMilliseconds`

Minimal example:

```json
{
  "MailCheck": {
    "ApiKey": "your-mailcheck-api-key",
    "BaseUrl": "https://api.mailcheck.ai",
    "VerifyEndpointTemplate": "/email/{email}",
    "TimeoutSeconds": 10,
    "MaxAttempts": 2,
    "RetryBaseDelayMilliseconds": 250
  }
}
```

If Mailcheck is not configured, the application will block new registrations rather than silently accepting unvalidated addresses.

## Running Locally

From the repository root:

```bash
cd EVotingSystem
dotnet run
```

If you are working in a restricted environment where the .NET CLI cannot write to the default home directory, use:

```bash
cd EVotingSystem
DOTNET_CLI_HOME=/tmp dotnet run
```

Local behavior notes:

- with Firebase configured, the app uses Firestore-backed data
- without Firebase configured, the project still supports a demo-friendly local experience using seeded configuration data
- development logging is more verbose than production logging

## Testing

Run the full solution build:

```bash
DOTNET_CLI_HOME=/tmp dotnet build ElectionPlatform.slnx
```

Run the automated tests:

```bash
DOTNET_CLI_HOME=/tmp dotnet test ElectionPlatform.slnx --no-build
```

The test suite covers important areas including:

- account controller behavior
- voting workflow logic
- results calculations
- Mailcheck validation behavior
- Firestore transaction-related functionality

## Deployment Notes

The application has been prepared for standard ASP.NET hosting environments and cloud platforms. Key deployment expectations include:

- set `ASPNETCORE_ENVIRONMENT=Production`
- terminate or enforce HTTPS correctly
- configure forwarded headers when running behind IIS, Nginx, App Service, or another reverse proxy
- store secrets outside source control
- deploy the full published output, including `wwwroot`
- keep `AllowedHosts` restricted to real hostnames
- disable startup seeding for production unless intentionally using demo data

For the full production checklist and hosting guidance, see [EVotingSystem/Docs/deployment-guide.md](/workspaces/systems-dev/EVotingSystem/Docs/deployment-guide.md).

## Screenshots

Add final screenshots here before submission or portfolio review:

- `Home page placeholder`
- `Registration page placeholder`
- `Login page placeholder`
- `Ballot page placeholder`
- `Vote confirmation / already-voted state placeholder`
- `Public results dashboard placeholder`

## Future Improvements

- introduce role-based administration for election setup and monitoring
- add auditable event logging and richer observability
- strengthen identity features with email confirmation and optional MFA
- add dashboards for voter participation by region and time window
- support scheduled election activation and closure workflows
- expand automated test coverage for more infrastructure edge cases
- add containerization and CI/CD pipeline templates
- improve accessibility verification and automated UI testing

## Academic Brief Alignment Summary

This project aligns strongly with a typical honours-level systems development brief by demonstrating:

- a working MVC web application with clear user journeys
- application of object-oriented design and layered architecture
- integration with a real external cloud datastore
- integration with a real external email-quality service
- security-aware handling of authentication, cookies, antiforgery, and rate limiting
- careful consideration of transaction integrity in the voting workflow
- environment-aware configuration and deployment readiness
- documentation that explains technical decisions, trade-offs, and operational concerns

In short, the Election Platform is not just a UI prototype. It is a credible systems-development submission that shows architectural thinking, implementation discipline, and readiness for both academic evaluation and professional review.
