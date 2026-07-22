# Preserve Community integrity

## Overview

Community Starter is a community platform divided into product and platform subsystems. The
Communities and membership subsystem owns this feature.

*preserve Community integrity* — subsystem capability that covers isolate Community state and enforce limits and concurrency

Accounts organize around distinct Communities. Each Community owns its Memberships, Roles, Permissions, Spaces, settings, and lifecycle, and the server preserves administrative continuity and strict multi-community tenancy through every transition. The platform shall scope Community operations, quotas, lists, and events to their owning Community and resolve concurrent Membership changes consistently.

The feature groups 2 traced behaviors behind one policy and evidence
boundary: `L2-COMM-011` and `L2-COMM-012`. Authoritative state commits before projections, delivery, or external work reports
success.

## Description

The repository contains specifications but no application implementation. This greenfield slice
defines the following building blocks across `Member Web Application`, `Community API`, the
application and domain layer, and infrastructure.

- **`PreserveCommunityIntegritySurface`** — page component in `Member Web Application`. It presents current
  state, submits user intent, and reconciles the typed result.
- **`PreserveCommunityIntegrityClient`** — typed Angular client. It creates `PreserveCommunityIntegrityRequest` values and maps stable
  transport failures into feature results.
- **`PreserveCommunityIntegrityEndpoint`** — HTTP endpoint in `Community API`. It authenticates the
  caller, applies boundary policy, and dispatches the request.
- **`PreserveCommunityIntegrityRequest`** — immutable request carrying `SubjectId`, `Action`, `ExpectedVersion`, and the
  scoped input needed by one traced behavior.
- **`PreserveCommunityIntegrityHandler`** — application service that loads authorized state through
  `IPreserveCommunityIntegrityRepository`, invokes `PreserveCommunityIntegrityPolicy`, and commits an accepted transition.
- **`PreserveCommunityIntegrityPolicy`** — domain policy that evaluates current state and returns a typed
  `PreserveCommunityIntegrityDecision` without performing external work.
- **`PreserveCommunityIntegrityRecord`** — authoritative record containing the feature state, scope, and concurrency
  version.
- **`IPreserveCommunityIntegrityRepository`** — persistence port that loads scoped state and commits one conditional
  unit of work.
- **`PreserveCommunityIntegrityProjector`** — idempotent post-commit component in `Community Job Worker`. It updates
  eligible projections and invokes configured external providers.

`PreserveCommunityIntegrityPolicy` exposes one named operation for each traced behavior:

- **`PreserveCommunityIntegrityPolicy.IsolateCommunityState(record, request)`** — evaluates `L2-COMM-011` (isolate Community state) and returns a typed decision before any state change.
- **`PreserveCommunityIntegrityPolicy.EnforceLimitsAndConcurrency(record, request)`** — evaluates `L2-COMM-012` (enforce limits and concurrency) and returns a typed decision before any state change.

## Requirements

The feature realizes the following level-2 (L2) requirements. Each row preserves the specification
identifier, its level-1 (L1) parent, and the requirement statement verbatim.

| L2 ID | Refines (L1) | Requirement |
|-------|--------------|-------------|
| `L2-COMM-011` | `L1-COMM-004` | Community settings, Memberships, Roles, Permissions, Posts, Comments, Attachments, Feeds, and Search results are selected and mutated only within the server-established Community boundary. |
| `L2-COMM-012` | `L1-COMM-004` | Community and Membership limits are server-owned, measured within the correct Community, and enforced atomically under retries and concurrent changes. |

## Diagrams

### System context

The `Community Member` uses `Community Platform` for the feature. The system invokes
`Delivery and Storage Providers` only for configured external work after authoritative decisions.

![C4 system context for preserve Community integrity](diagrams/c4-context.png)

### Containers

`Member Web Application` collects intent, `Community API` applies the synchronous boundary,
and `Community Database` holds authoritative state. `Community Job Worker` handles eligible
post-commit work against `Delivery and Storage Providers`.

![C4 container view for preserve Community integrity](diagrams/c4-container.png)

### Components

Inside `Community API`, `PreserveCommunityIntegrityEndpoint` dispatches `PreserveCommunityIntegrityHandler`. The handler evaluates
`PreserveCommunityIntegrityPolicy`, persists through `IPreserveCommunityIntegrityRepository`, and hands committed outcomes to
`PreserveCommunityIntegrityProjector`.

![C4 component view for preserve Community integrity](diagrams/c4-component.png)

### Class structure

`PreserveCommunityIntegrityHandler` depends on the immutable request, domain policy, and repository port.
`PreserveCommunityIntegrityRecord` owns versioned state, while `PreserveCommunityIntegrityProjector` consumes committed results.

![Class diagram for preserve Community integrity](diagrams/class-structure.png)

### Behaviour — isolate Community state

The interaction loads current scoped state before `PreserveCommunityIntegrityPolicy` enforces
`L2-COMM-011`. Rejected decisions return without changing authoritative state; accepted
state changes commit before optional derived work starts.

![Sequence diagram for isolate Community state](diagrams/sequence-l2-comm-011.png)

### Behaviour — enforce limits and concurrency

The interaction loads current scoped state before `PreserveCommunityIntegrityPolicy` enforces
`L2-COMM-012`. Rejected decisions return without changing authoritative state; accepted
state changes commit before optional derived work starts.

![Sequence diagram for enforce limits and concurrency](diagrams/sequence-l2-comm-012.png)
