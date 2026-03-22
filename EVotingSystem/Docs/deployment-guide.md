# Election Platform Deployment Guide

## Production readiness checklist

- Set `ASPNETCORE_ENVIRONMENT=Production`
- Use HTTPS in front of the app
- Provide Firebase and Mailcheck secrets through environment variables or a secret manager, not committed config
- Set `AllowedHosts` to your real hostname(s)
- Disable startup demo seeding in production unless you are deliberately provisioning a demo environment
- Verify Firestore collections exist and the service account has the minimum required permissions
- Confirm registration, login, ballot submission, and public results work against production configuration
- Validate proxy forwarding if hosted behind IIS, Nginx, Azure App Service, or another reverse proxy
- Review logs to ensure no secrets or raw backend responses are being written
- Publish static assets and verify CSS, JS, and image files are served correctly

## Development vs Production configuration separation

### Development

- `appsettings.Development.json` can keep verbose logging
- local demo mode may run without Firebase credentials
- startup seeding is useful locally

### Production

- use [appsettings.Production.json](/workspaces/systems-dev/EVotingSystem/appsettings.Production.json) only for non-secret overrides
- use [appsettings.Production.template.json](/workspaces/systems-dev/EVotingSystem/appsettings.Production.template.json) as the structure reference
- keep Firebase and Mailcheck secrets out of source control
- prefer `Firestore:SeedOnStartup=false`

## Secure secret handling guidance

Do not store these in committed files:

- `Firebase:ProjectId`
- `Firebase:ServiceAccountEmail`
- `Firebase:ServiceAccountPrivateKey`
- `MailCheck:ApiKey`

Recommended secret sources:

- cloud secret manager
- hosting platform application settings
- environment variables
- user secrets for local development only

## Firebase credential deployment guidance

This app uses service-account style values rather than a downloaded JSON file at runtime. You should deploy:

- `Firebase__ProjectId`
- `Firebase__DatabaseId`
- `Firebase__ServiceAccountEmail`
- `Firebase__ServiceAccountPrivateKey`
- `Firebase__TokenUri`

Important:

- keep the private key PEM text intact
- if your platform requires single-line environment variables, preserve newlines as `\n`
- use a service account with only the Firestore permissions needed for candidate reads, vote writes, and stats updates

## ASP.NET hosting guidance

This app is suitable for:

- IIS / ASP.NET Core Module
- Azure App Service
- container hosting
- Linux + Nginx reverse proxy + Kestrel

Production-safe hosting points already applied:

- HSTS outside development
- secure auth cookies
- forwarded-header handling for reverse-proxy deployments
- global anti-forgery validation for unsafe HTTP methods
- rate limiting on authentication and vote submission

## Static asset handling

- static files under `wwwroot/` are served directly
- production responses now set cache headers for static assets
- continue using `asp-append-version="true"` on CSS/JS so deployments invalidate old browser cache automatically
- keep candidate images and branding assets in `wwwroot/images/`

## Logging considerations

- keep production log levels at `Warning` by default unless diagnosing an issue
- centralize logs in your hosting platform or aggregator if possible
- avoid logging raw secrets, tokens, private keys, or full unmasked user emails
- review startup logs for placeholder Firebase warnings or repeated Mailcheck / Firestore failures

## Common deployment pitfalls and fixes

### Problem: Firebase is configured in code but still not used

Fix:

- ensure all Firebase values are present
- make sure they are not placeholder values
- confirm the private key is formatted correctly

### Problem: Registration fails in production

Fix:

- check `MailCheck__ApiKey`
- verify outbound network access to the Mailcheck API
- confirm timeouts or rate limits are not being hit

### Problem: HTTPS redirect loops or wrong scheme

Fix:

- confirm reverse proxy forwarding is enabled
- make sure `X-Forwarded-Proto` is passed to the app

### Problem: Static assets look stale after deployment

Fix:

- ensure published `wwwroot` files were deployed
- confirm the browser is receiving versioned asset URLs
- clear CDN or reverse-proxy cache if used

### Problem: Votes do not persist

Fix:

- check Firestore permissions for the service account
- verify the configured collection names
- confirm the election stats document exists or reseed the environment intentionally

## Deployment plan

1. Prepare the target environment with HTTPS, hostname, and application settings.
2. Set `ASPNETCORE_ENVIRONMENT=Production`.
3. Configure Firebase and Mailcheck secrets through environment variables or a secret store.
4. Deploy the published ASP.NET Core app and static assets.
5. Validate startup logs for configuration and connectivity issues.
6. Verify guest results, registration, login, ballot load, and vote submission.
7. Seed or provision production candidate/stat data intentionally.
8. Monitor logs during the first smoke test and adjust log routing if needed.

## Environment variable list

- `ASPNETCORE_ENVIRONMENT=Production`
- `ASPNETCORE_URLS`
- `AllowedHosts`
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
- `MailCheck__ApiKey`
- `MailCheck__BaseUrl`
- `MailCheck__VerifyEndpointTemplate`
- `MailCheck__TimeoutSeconds`
- `MailCheck__MaxAttempts`
- `Ui__ApplicationName`
- `Ui__SupportEmail`

## Markers and operators should notice

- clean separation of development/demo config from production config
- secret values are expected from secure deployment sources
- the app degrades safely when Firestore or Mailcheck are unavailable
- hosting guidance is aligned with standard ASP.NET Core deployment patterns
