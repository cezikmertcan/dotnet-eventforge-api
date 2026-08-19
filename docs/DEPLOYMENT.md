# Deployment Guide

## Container deployment

The image listens on port `8080` and runs as the non-root application user from the official ASP.NET runtime image.

```bash
cp .env.example .env
# Set a strong, unique JWT_SIGNING_KEY and real service connection strings.
docker compose up --build -d
curl http://localhost:8080/health/ready
```

For a real deployment, replace the local Compose MongoDB and Redis services with managed instances or secured private-network services. Do not expose either data service directly to the public internet.

## Required production values

Set these values through the platform's secret/configuration facility:

- `MONGO_CONNECTION_STRING`
- `MONGO_DATABASE_NAME`
- `REDIS_CONNECTION_STRING`
- `JWT_ISSUER`
- `JWT_AUDIENCE`
- `JWT_SIGNING_KEY`

The application deliberately fails fast when production MongoDB, Redis, or JWT configuration is missing. A `.env` file is a local-development convenience, not a production secret-management strategy.

## Health checks

Use `/health/live` for process liveness and `/health/ready` for traffic readiness. A deployment should only route traffic to instances whose readiness endpoint returns `200`.

## Release checklist

- Run the complete build and test commands.
- Scan the repository for accidental secrets.
- Confirm the JWT signing key is unique and rotated appropriately.
- Confirm MongoDB and Redis are private and authenticated.
- Review the Swagger contract for unintended public endpoints.
- Configure HTTPS and platform-level request logging/rate controls.
