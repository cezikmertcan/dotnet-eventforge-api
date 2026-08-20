# EventForge API

[![CI](https://github.com/cezikmertcan/eventforge-api/actions/workflows/ci.yml/badge.svg)](https://github.com/cezikmertcan/eventforge-api/actions/workflows/ci.yml)

EventForge is a production-minded .NET 10 backend for planning and running community events. It demonstrates a related MongoDB document model, JWT authentication with roles, Redis cache-aside reads, Dockerized local infrastructure, and a documented API surface.


## What it demonstrates

- .NET 10 ASP.NET Core controllers and dependency injection
- MongoDB document persistence with unique and relationship-oriented indexes
- Redis cache-aside reads with graceful cache degradation and invalidation
- JWT access tokens with `Admin`, `Organizer`, and `Attendee` roles
- BCrypt password hashing and environment-driven admin bootstrap
- CRUD for five related documents: venues, events, speakers, sessions, and registrations
- Reference validation across the domain model
- Swagger/OpenAPI with a JWT Bearer security scheme
- Liveness/readiness checks, ProblemDetails, duplicate-key handling, security headers, and IP rate limiting
- Docker Compose for the API, MongoDB, and Redis
- GitHub Actions build, test, coverage, and container checks

## Domain model

```text
Venue  <-  Event  ->  Registration  ->  User
             |
           Session  ->  Speaker
```

| Document | Relationship | Purpose |
| --- | --- | --- |
| `Venue` | referenced by `Event` | Capacity and location information |
| `Event` | references `Venue` and `User` | A scheduled event owned by an organizer |
| `Speaker` | referenced by `Session` | Speaker profile and topics |
| `Session` | references `Event` and `Speaker[]` | A scheduled talk or workshop |
| `Registration` | references `Event` and `User` | An attendee's event registration |
| `User` | owns events and registrations | Authentication identity and role |

## API surface

| Area | Endpoints | Access |
| --- | --- | --- |
| Authentication | `POST /api/auth/register`, `POST /api/auth/login`, `GET /api/auth/me` | Register/login public; `me` authenticated |
| Users | `GET /api/users`, `GET /api/users/{id}`, `PATCH /api/users/{id}/role` | Admin |
| Venues | `GET`, `POST`, `PUT`, `DELETE /api/venues` | Read authenticated; write Admin/Organizer |
| Events | `GET`, `POST`, `PUT`, `DELETE /api/events` | Read authenticated; write Admin/Organizer |
| Speakers | `GET`, `POST`, `PUT`, `DELETE /api/speakers` | Read authenticated; write Admin/Organizer |
| Sessions | `GET`, `POST`, `PUT`, `DELETE /api/sessions` | Read authenticated; write Admin/Organizer |
| Registrations | `GET`, `POST`, `PUT`, `DELETE /api/registrations` | Attendee-owned; staff can manage event registrations |

Additional endpoints:

- `GET /` — English project landing page
- `GET /api/meta` — machine-readable service metadata
- `GET /health/live` — process liveness
- `GET /health/ready` — MongoDB and Redis readiness
- `/swagger` — interactive OpenAPI documentation

## Role model

- `Admin`: manage users, roles, and all event resources
- `Organizer`: manage venues, events, speakers, sessions, and event registrations
- `Attendee`: browse authenticated resources and manage their own registrations

Registration ownership is derived from the JWT subject; clients cannot submit another user's ID.

## Run locally

Requirements: .NET 10 SDK, Docker Desktop, and Git.

```bash
cp .env.example .env
docker compose up -d mongo redis
dotnet run --project src/EventForge.Api --urls http://localhost:8080
```

Open `http://localhost:8080/` for the project page or `http://localhost:8080/swagger` for the API explorer.

To run the complete stack in containers:

```bash
cp .env.example .env
# Set JWT_SIGNING_KEY in .env to a unique value.
docker compose up --build
```

The optional `SEED_ADMIN_EMAIL` and `SEED_ADMIN_PASSWORD` values create or promote one admin at startup. Leave them blank when bootstrap seeding is not needed.

## Configuration

Production configuration is environment-variable driven. The application intentionally does not fall back to localhost MongoDB or Redis outside Development.

| Variable | Purpose |
| --- | --- |
| `MONGO_CONNECTION_STRING` | MongoDB connection string |
| `MONGO_DATABASE_NAME` | MongoDB database name |
| `REDIS_CONNECTION_STRING` | Redis connection string |
| `REDIS_INSTANCE_NAME` | Redis key prefix |
| `JWT_ISSUER` / `JWT_AUDIENCE` | JWT validation metadata |
| `JWT_SIGNING_KEY` | At least 32-character signing key |
| `JWT_ACCESS_TOKEN_MINUTES` | Access token lifetime |
| `SEED_ADMIN_*` | Optional one-time admin bootstrap settings |

Never commit `.env`, real credentials, JWT keys, or production connection strings.

## Verification

```bash
dotnet build EventForge.slnx --configuration Release
dotnet test EventForge.slnx --configuration Release --collect:"XPlat Code Coverage"
docker build --tag eventforge-api:local .
```

The CI workflow runs the same build/test/container gates on pushes and pull requests.

## Project guides

- [Architecture](docs/ARCHITECTURE.md)
- [Deployment](docs/DEPLOYMENT.md)
- [Security policy](SECURITY.md)
- [Contribution guide](CONTRIBUTING.md)

## License

This project is licensed under the MIT License.
