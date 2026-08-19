# Architecture

## Runtime layers

```text
HTTP client
   |
ASP.NET Core middleware
   |-- ProblemDetails / security headers / rate limiting
   |-- JWT authentication and role authorization
   |
Controllers
   |-- request validation and relationship checks
   |-- cache-aside reads and invalidation
   |
MongoRepository<T>       RedisCacheService
   |                      |
MongoDB                  Redis
```

## Persistence

MongoDB stores each aggregate as a document collection. The API keeps references explicit rather than embedding every related resource:

- `events.venueId` points to `venues._id`.
- `sessions.eventId` points to `events._id`.
- `sessions.speakerIds` points to `speakers._id` values.
- `registrations.eventId` and `registrations.attendeeId` point to the event and user.

The startup index initializer creates a unique email index, a unique event slug index, schedule indexes, and a unique event/attendee registration index.

## Caching

List and detail reads use Redis cache-aside behavior. Writes invalidate the affected detail key and relevant list/relationship keys. Redis failures are logged and treated as cache misses so the API can continue serving MongoDB-backed data; readiness still reports Redis health accurately.

## Authentication

Passwords are stored as BCrypt hashes. Login issues a short-lived JWT containing the user ID, email, display name, and role. Registration always creates an `Attendee`; an `Admin` can promote users through the protected role endpoint.

## Operational endpoints

- `/health/live` confirms the process can accept requests without requiring dependencies.
- `/health/ready` checks MongoDB and Redis connectivity.
- `/swagger` exposes the OpenAPI contract and Bearer-token authorization helper.
