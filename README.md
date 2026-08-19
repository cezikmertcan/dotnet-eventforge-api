# EventForge API

EventForge is a production-minded .NET 10 API for managing community events, venues, speakers, sessions, and attendee registrations.

> The repository is being built incrementally as a portfolio-quality backend example. It starts private and will only be made public after a final secrets and security review.

## Planned capabilities

- JWT authentication with `Admin`, `Organizer`, and `Attendee` roles
- MongoDB document storage with related domain documents
- Redis cache-aside reads and explicit cache invalidation
- CRUD endpoints for venues, events, speakers, sessions, and registrations
- OpenAPI/Swagger documentation
- Health checks, structured errors, rate limiting, and secure configuration
- Docker Compose for the API, MongoDB, and Redis
- Automated build, test, coverage, and container checks in GitHub Actions

## Domain model

The API models an event platform rather than a generic sample CRUD app:

```text
Venue  <- Event -> Registration
             |
           Session -> Speaker
```

The complete setup and endpoint documentation will be expanded as each feature is added.

## Local development

Requirements: .NET 10 SDK, Docker Desktop, and Git.

```bash
cp .env.example .env
docker compose up -d mongo redis
dotnet run --project src/EventForge.Api --urls http://localhost:8080
```

The API will be available at `http://localhost:8080`. Swagger will be enabled as the API surface is implemented.

## Configuration and security

`.env` is intentionally ignored by Git. Use environment variables or a managed secret store in deployed environments. Never commit real JWT keys, admin passwords, database credentials, or production connection strings.

## License

This project is licensed under the MIT License.
