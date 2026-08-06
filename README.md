# UrlShortener

UrlShortener is a lightweight URL shortening service built with ASP.NET Core. It supports user authentication, short-link management, redirect tracking, and basic admin analytics. The project is intentionally practical rather than over-engineered: it uses a familiar layered API structure, Entity Framework Core for persistence, Redis for caching, and ASP.NET Core Identity for authentication.

## What the project does

The application allows users to:

- create short URLs from long URLs
- view, update, and delete their own short links
- redirect visitors through a short code endpoint
- track basic click analytics such as browser, operating system, IP address, and timestamp
- authenticate with JWTs and refresh tokens
- access admin-only system analytics

## Architecture at a glance

The project follows a simple web API structure:

- Controllers handle HTTP requests and return API responses
- Services contain the core business behavior
- EF Core models and the DbContext manage persistence
- Redis provides caching for frequently requested analytics data
- ASP.NET Core Identity manages users, roles, and password handling

### Main layers

- Presentation: controllers under the Controllers folder
- Application services: AuthService, AnalyticService, DashboardService, TokenServices
- Data access: UrlShortenerContext plus the model classes in the Models folder
- Cross-cutting concerns: exception middleware, authentication/authorization policies, rate limiting, caching, and Swagger

## Core domain model

The application centers around three main entities:

- ShortUrl: stores the original URL, the generated short code, ownership, timestamps, expiration, and click count
- ClickAnalytic: stores a click event for a short URL, including browser, OS, IP, and time
- ApplicationUser: extends ASP.NET Core Identity with profile fields and a relationship to the user’s URLs

This model keeps the system easy to understand while still supporting useful analytics.

## Request flow

### Creating a short URL

1. A user sends a URL to the URL API endpoint.
2. The controller validates and sanitizes the input.
3. A short code is generated and stored in the database.
4. The API returns the created short-link record.

### Redirecting a short URL

1. The redirect endpoint resolves the short code.
2. The click count is incremented.
3. A click event is recorded with browser and OS information.
4. The visitor is redirected to the original destination.

### Authentication flow

1. The user logs in with email and password.
2. The server issues a JWT and stores a refresh token in a database-backed record and cookie.
3. The JWT is used for protected endpoints.
4. The refresh token can be used to mint a new access token when needed.

## Design decisions and tradeoffs

### 1. ASP.NET Core Identity for authentication

Using ASP.NET Core Identity was a strong choice because it gives the project a mature and secure foundation for user management, hashing, roles, and login flows.

Tradeoff:
- It is robust and well-supported, but it adds more moving parts than a minimal custom auth implementation.

### 2. JWTs with refresh tokens

The project uses JWTs for API access and refresh tokens to extend session lifetime without forcing the user to log in repeatedly.

Tradeoff:
- This is a practical and common approach for APIs, but it introduces more complexity than simple session-based authentication.
- The refresh token is still stored server-side, which improves control but increases persistence overhead.

### 3. Redis caching for analytics

Analytics and dashboard responses are cached to reduce repeated database reads and improve response times for heavy reporting workloads.

Tradeoff:
- Caching improves read performance, but it also introduces freshness concerns and invalidation complexity.
- The implementation uses short-lived cache entries, which keeps it simple but means analytics may be slightly stale for a few minutes.

### 4. Synchronous analytics logging on redirect

When a redirect happens, the click is recorded immediately in the database.

Tradeoff:
- This keeps the design straightforward and ensures analytics are persisted in real time.
- It does add latency to the redirect path, which could become a bottleneck under heavy traffic.
- A future version could move this to a background queue or event-driven pipeline.

### 5. URL sanitization and validation

Incoming URLs are sanitized before storage to reduce the chance of malformed or unsafe input.

Tradeoff:
- This improves safety and consistency, but it can also be overly strict for unusual but valid input.

### 6. Short codes generated in application code

Short codes are generated randomly from a character set.

Tradeoff:
- This is simple and dependency-light, but it does not guarantee uniqueness beyond the database constraint.
- For a larger-scale service, a more structured approach such as ULID or base62 generation could be more scalable and predictable.

## Technology choices

- ASP.NET Core Web API
- Entity Framework Core with MySQL
- ASP.NET Core Identity
- JWT authentication
- Redis distributed cache
- Swagger / OpenAPI for API exploration
- Rate limiting for basic abuse protection
- UAParser for browser and operating system detection

## Project structure

- Controllers: API endpoints
- Services: business logic and integration concerns
- Models: domain entities and DTOs
- Common: shared response wrapper
- Migrations: EF Core database changes

## Setup

This project expects a few environment variables to be available:

- DB_PASSWORD
- REDIS_CONNECTION_STRING
- JWT_SECRET_KEY
- JWT_AUDIENCE
- JWT_ISSUER
- JWT_LIFETIME

You will also need:

- a running MySQL instance
- a running Redis instance
- the .NET SDK compatible with the project target framework

A typical local development flow is:

1. configure the required environment variables
2. start MySQL and Redis
3. run database migrations
4. launch the application
5. open the Swagger UI for endpoint exploration

## Strengths of the current implementation

- clear separation between controllers and services
- practical authentication and authorization setup
- straightforward analytics flow
- easy to extend with more features
- good fit for a small-to-medium scale URL shortener service

## Where it could improve

- move analytics persistence to an asynchronous pipeline for higher throughput
- add cache invalidation strategies that are more explicit and robust
- introduce a dedicated URL service to reduce logic spread across controllers
- add more complete test coverage for authentication, redirect, and analytics behavior
- consider a more scalable storage and ID strategy for very high traffic

## Summary

This project is a solid example of a practical API-first URL shortener with user accounts, analytics, and security concerns handled in a straightforward way. Its biggest strength is that it stays easy to follow, while its biggest tradeoff is that some design choices are intentionally simple rather than optimized for very large-scale production traffic.
