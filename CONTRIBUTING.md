# Contributing to EventForge

## Development flow

1. Create a focused branch from `main`.
2. Keep API, data-model, and security changes scoped and documented.
3. Add or update tests for behavior changes.
4. Run the build, test, and Docker checks locally.
5. Open a pull request with the provided template.

## Local checks

```bash
dotnet build EventForge.slnx --configuration Release
dotnet test EventForge.slnx --configuration Release --collect:"XPlat Code Coverage"
docker build --tag eventforge-api:local .
```

## Data-model changes

MongoDB indexes are created by `MongoIndexInitializer`. Any new relationship should validate referenced IDs before writing and document cache invalidation behavior.

## Security expectations

Do not commit credentials, tokens, `.env` files, production URLs, or database dumps. Use the security policy for vulnerability reports.
