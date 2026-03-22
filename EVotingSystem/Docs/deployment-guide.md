# Election Platform Deployment Guide

## 1. Production readiness checklist

- Set `ASPNETCORE_ENVIRONMENT=Production`.
- Terminate TLS at the host or reverse proxy and keep HTTPS enabled end to end.
- Set `AllowedHosts` to the real public hostname list for the deployment.
- Keep `Firestore:SeedOnStartup=false` in production unless the target is an intentional demo instance.
- Supply Firebase and Mailcheck secrets through environment variables or a secret manager.
- Confirm outbound access to `oauth2.googleapis.com`, `firestore.googleapis.com`, and `api.mailcheck.ai`.
- Publish the full ASP.NET Core app, including `wwwroot/`.
- Smoke test registration, login, ballot load, vote submission, and public results.
- Review startup logs for configuration warnings before opening the app to users.
- Verify the Firestore service account has only the permissions required for this app.

## 2. Development vs Production configuration separation

### Development

- Keep shared defaults in [appsettings.json](/workspaces/systems-dev/EVotingSystem/appsettings.json).
- Put developer-only overrides in [appsettings.Development.json](/workspaces/systems-dev/EVotingSystem/appsettings.Development.json).
- Use user secrets or shell environment variables for local secrets.
- Verbose logging and startup seeding are acceptable locally.

### Production

- Keep only non-secret production overrides in [appsettings.Production.json](/workspaces/systems-dev/EVotingSystem/appsettings.Production.json).
- Use [appsettings.Production.template.json](/workspaces/systems-dev/EVotingSystem/appsettings.Production.template.json) as the final structure reference.
- Prefer environment variables or a secret store for all secret values.
- Keep seeding disabled and log levels conservative by default.

## 3. Secure secret handling guidance

Never commit these values:

- `Firebase:ProjectId`
- `Firebase:ServiceAccountEmail`
- `Firebase:ServiceAccountPrivateKey`
- `MailCheck:ApiKey`

Recommended secret sources:

- Azure App Service application settings
- AWS / GCP / platform secret manager
- container orchestrator secrets
- environment variables
- .NET user secrets for local development only

Operational notes:

- Rotate secrets after any accidental exposure.
- Limit who can read production secrets.
- Do not echo secrets in startup scripts or CI logs.
- Preserve the Firebase private key exactly; if the host only supports single-line values, store newlines as `\n`.

## 4. Firebase credential deployment guidance

This app authenticates to Firestore with service-account values, not a downloaded JSON file consumed directly at runtime. Configure:

- `Firebase__ProjectId`
- `Firebase__DatabaseId`
- `Firebase__ServiceAccountEmail`
- `Firebase__ServiceAccountPrivateKey`
- `Firebase__TokenUri`

Deployment guidance:

- Use a dedicated service account for the app.
- Grant only the Firestore permissions needed for reads, writes, and transaction commits used by the platform.
- Confirm the target Firestore database and collection names match the configured values.
- Test the credential formatting before cutover by validating a read and a write in a staging environment.

## 5. ASP.NET hosting guidance

Supported hosting patterns:

- IIS with ASP.NET Core Module
- Azure App Service
- Linux VM or container with Nginx or Apache in front of Kestrel
- generic cloud platform running ASP.NET Core

Hosting expectations already baked into the app:

- forwarded headers support for reverse proxies
- HTTPS redirection
- HSTS outside development
- antiforgery validation on unsafe form posts
- secure auth and antiforgery cookies
- rate limiting for login and vote submission
- conservative security headers on responses
- static asset cache control

Publish command:

```bash
dotnet publish /workspaces/systems-dev/EVotingSystem/EVotingSystem.csproj -c Release -o ./publish
```

Important reverse-proxy note:

- forwarded headers must reach the app so the request scheme is recognized correctly
- if your host strips `X-Forwarded-Proto`, you can see HTTPS redirect loops

## 6. Static asset handling

- All web assets must be deployed from `wwwroot/`.
- CSS and JS already use `asp-append-version="true"` for cache busting.
- Versioned static assets now receive long-lived immutable caching in production.
- Non-versioned static assets receive a shorter cache lifetime.
- Keep candidate images, logos, and branded artwork inside `wwwroot/images/`.

Operational advice:

- publish the updated `wwwroot` content with every release
- if a CDN is used, invalidate cached non-versioned assets after deployment
- avoid referencing local development-only asset paths

## 7. Logging considerations

- Production defaults should stay at `Warning` unless you are diagnosing an incident.
- Console logging is appropriate for platform log capture; debug logging should stay development-only.
- Never log secrets, bearer tokens, private keys, or full unmasked personal data.
- Review warnings about `AllowedHosts`, missing Firebase config, missing Mailcheck config, or unexpected startup seeding.
- Centralize logs through the host platform when possible.

## 8. Common deployment pitfalls and fixes

### HTTPS redirect loop behind a proxy

Cause:

- `X-Forwarded-Proto` is missing or not trusted by the host chain

Fix:

- ensure the reverse proxy forwards the scheme header
- keep forwarded-header processing enabled before HTTPS redirection

### Registration fails immediately

Cause:

- `MailCheck__ApiKey` is missing or outbound access to `api.mailcheck.ai` is blocked

Fix:

- add the API key through secure configuration
- verify egress/network rules and DNS resolution

### Firestore reads or writes fail

Cause:

- malformed private key, wrong project ID, wrong database ID, or insufficient service-account permissions

Fix:

- verify each Firebase value
- confirm newline preservation in the private key
- test the service account against the target Firestore project

### Demo data appears in production

Cause:

- `Firestore__SeedOnStartup=true` was left enabled

Fix:

- disable seeding in production
- clean up any demo documents that were inserted unintentionally

### Stale CSS, JS, or images after release

Cause:

- old cached assets at the browser, CDN, or reverse proxy

Fix:

- rely on versioned URLs for CSS and JS
- invalidate non-versioned cached assets if a CDN or proxy is in front

## Deployment plan

1. Provision the target host, DNS, TLS certificate, and reverse-proxy configuration.
2. Set production environment variables and secret-store entries.
3. Confirm `AllowedHosts` and `Firestore__SeedOnStartup=false`.
4. Publish the application and deploy the output, including `wwwroot/`.
5. Start the app and review startup warnings and connectivity logs.
6. Run smoke tests for registration, login, vote submission, and results.
7. Verify Firestore documents are written correctly and only once per vote.
8. Monitor logs and resource usage during the first live traffic window.

## Environment variable list

Required:

- `ASPNETCORE_ENVIRONMENT=Production`
- `AllowedHosts`
- `Firebase__ProjectId`
- `Firebase__DatabaseId`
- `Firebase__ServiceAccountEmail`
- `Firebase__ServiceAccountPrivateKey`
- `Firebase__TokenUri`
- `MailCheck__ApiKey`
- `Firestore__SeedOnStartup=false`

Common optional overrides:

- `ASPNETCORE_URLS`
- `Logging__LogLevel__Default`
- `Logging__LogLevel__Microsoft.AspNetCore`
- `Logging__LogLevel__Microsoft.AspNetCore.Identity`
- `Firestore__Collections__Elections`
- `Firestore__Collections__Candidates`
- `Firestore__Collections__Votes`
- `Firestore__Collections__ElectionStats`
- `Firestore__Collections__VoterProfiles`
- `MailCheck__BaseUrl`
- `MailCheck__VerifyEndpointTemplate`
- `MailCheck__TimeoutSeconds`
- `MailCheck__MaxAttempts`
- `MailCheck__RetryBaseDelayMilliseconds`
- `Ui__ApplicationName`
- `Ui__SupportEmail`

## Final appsettings template

Use [appsettings.Production.template.json](/workspaces/systems-dev/EVotingSystem/appsettings.Production.template.json) as the deployment template. Secret values should come from environment variables or a secret store even if this file is copied to create a host-local `appsettings.Production.json`.

## Required production-safety changes now applied

- forwarded headers are processed before HTTPS redirection to avoid proxy redirect loops
- debug logging is now development-only
- production startup warnings now call out unsafe `AllowedHosts`, enabled seeding, and missing Firebase or Mailcheck configuration
- response compression is enabled
- security headers are applied to responses
- versioned static assets now receive immutable cache headers in production
