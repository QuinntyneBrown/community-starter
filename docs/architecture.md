# Architecture

Community Starter begins as a cohesive modular monolith. Product policy is isolated from protocols,
providers, and deployment while the initial artifact stays inexpensive to operate.

## Dependency direction

`Api -> Infrastructure -> Application -> Domain`. Domain has no framework dependency. Application
declares ports and coordinates use cases. Infrastructure implements persistence and providers. API
owns HTTP, authentication, realtime, health, and composition.

## Authoritative change sequence

1. Authenticate and resolve the current Account and Session.
2. Load current Community-scoped state.
3. Evaluate the named feature policy and current authorization/effect matrices.
4. Commit the state transition, audit record, outbox messages, and jobs atomically.
5. Return the committed representation.
6. Project and deliver derived effects idempotently after commit.

## Decisions

- PostgreSQL 18 is authoritative and also hosts the initial durable queue and search projections.
- Sessions are opaque and server-side; cookies are Secure, HttpOnly, and SameSite=Lax.
- Public marketing is statically generated; `/app` is Angular; `/api` and `/hubs` never fall back to HTML.
- One OCI image runs as `api`, `worker`, or `all`; the first deployment uses `all` without horizontal claims.
- Provider code is behind Application ports. Production channels fail closed when incomplete.
- Generated feature contracts preserve all 260 requirement identifiers; executable journeys keep
  direct evidence in the Domain, Application, API integration, and Angular test projects.
