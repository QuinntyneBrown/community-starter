# Consistent attendance management

## Overview

Community Starter is a community platform divided into product and platform subsystems. The
Community events subsystem owns this feature.

*consistent attendance management* — subsystem capability that covers transition one current RSVP, allocate capacity and waitlist fairly, and reconcile attendance after access loss

Communities need to schedule activities, control who can discover and attend them, coordinate finite capacity, and communicate changes across time zones. Event and RSVP rules are server-owned and shall remain correct under concurrent requests, cancellation, privacy, and moderation. The platform shall manage RSVP, capacity, waitlist, and organizer attendance operations without oversubscription, duplicate effects, or disclosure to unauthorized Accounts.

The feature groups 3 traced behaviors behind one policy and evidence
boundary: `L2-EVNT-007`, `L2-EVNT-008`, and `L2-EVNT-013`. Authoritative state commits before projections, delivery, or external work reports
success.

## Description

The repository contains specifications but no application implementation. This greenfield slice
defines the following building blocks across `Member Web Application`, `Community API`, the
application and domain layer, and infrastructure.

- **`ManageAttendanceSurface`** — page component in `Member Web Application`. It presents current
  state, submits user intent, and reconciles the typed result.
- **`ManageAttendanceClient`** — typed Angular client. It creates `ManageAttendanceRequest` values and maps stable
  transport failures into feature results.
- **`ManageAttendanceEndpoint`** — HTTP endpoint in `Community API`. It authenticates the
  caller, applies boundary policy, and dispatches the request.
- **`ManageAttendanceRequest`** — immutable request carrying `SubjectId`, `Action`, `ExpectedVersion`, and the
  scoped input needed by one traced behavior.
- **`ManageAttendanceHandler`** — application service that loads authorized state through
  `IManageAttendanceRepository`, invokes `ManageAttendancePolicy`, and commits an accepted transition.
- **`ManageAttendancePolicy`** — domain policy that evaluates current state and returns a typed
  `ManageAttendanceDecision` without performing external work.
- **`ManageAttendanceRecord`** — authoritative record containing the feature state, scope, and concurrency
  version.
- **`IManageAttendanceRepository`** — persistence port that loads scoped state and commits one conditional
  unit of work.
- **`ManageAttendanceProjector`** — idempotent post-commit component in `Community Job Worker`. It updates
  eligible projections and invokes configured external providers.

`ManageAttendancePolicy` exposes one named operation for each traced behavior:

- **`ManageAttendancePolicy.TransitionOneCurrentRSVP(record, request)`** — evaluates `L2-EVNT-007` (transition one current RSVP) and returns a typed decision before any state change.
- **`ManageAttendancePolicy.AllocateCapacityAndWaitlistFairly(record, request)`** — evaluates `L2-EVNT-008` (allocate capacity and waitlist fairly) and returns a typed decision before any state change.
- **`ManageAttendancePolicy.ReconcileAttendanceAfterAccessLoss(record, request)`** — evaluates `L2-EVNT-013` (reconcile attendance after access loss) and returns a typed decision before any state change.

## Requirements

The feature realizes the following level-2 (L2) requirements. Each row preserves the specification
identifier, its level-1 (L1) parent, and the requirement statement verbatim.

| L2 ID | Refines (L1) | Requirement |
|-------|--------------|-------------|
| `L2-EVNT-007` | `L1-EVNT-003` | One eligible Member with an active Community Membership has one current RSVP per Event, transitioned idempotently under Event, audience, Space/rules, deadline, capacity, Block, and Moderation state. |
| `L2-EVNT-008` | `L1-EVNT-003` | Finite capacity and waitlist promotion are transactional, deterministic, observable, and safe under concurrent RSVP, withdrawal, capacity change, and retry. |
| `L2-EVNT-013` | `L1-EVNT-003` | Attendance is reconciled from current Event access so a removed, blocked, banned, or otherwise ineligible Account cannot retain a place, receive restricted details, or block waitlist progress. |

## Diagrams

### System context

The `Event Organizer` uses `Community Platform` for the feature. The system invokes
`Delivery and Storage Providers` only for configured external work after authoritative decisions.

![C4 system context for consistent attendance management](diagrams/c4-context.png)

### Containers

`Member Web Application` collects intent, `Community API` applies the synchronous boundary,
and `Community Database` holds authoritative state. `Community Job Worker` handles eligible
post-commit work against `Delivery and Storage Providers`.

![C4 container view for consistent attendance management](diagrams/c4-container.png)

### Components

Inside `Community API`, `ManageAttendanceEndpoint` dispatches `ManageAttendanceHandler`. The handler evaluates
`ManageAttendancePolicy`, persists through `IManageAttendanceRepository`, and hands committed outcomes to
`ManageAttendanceProjector`.

![C4 component view for consistent attendance management](diagrams/c4-component.png)

### Class structure

`ManageAttendanceHandler` depends on the immutable request, domain policy, and repository port.
`ManageAttendanceRecord` owns versioned state, while `ManageAttendanceProjector` consumes committed results.

![Class diagram for consistent attendance management](diagrams/class-structure.png)

### Behaviour — transition one current RSVP

The interaction loads current scoped state before `ManageAttendancePolicy` enforces
`L2-EVNT-007`. Rejected decisions return without changing authoritative state; accepted
state changes commit before optional derived work starts.

![Sequence diagram for transition one current RSVP](diagrams/sequence-l2-evnt-007.png)

### Behaviour — allocate capacity and waitlist fairly

The interaction loads current scoped state before `ManageAttendancePolicy` enforces
`L2-EVNT-008`. Rejected decisions return without changing authoritative state; accepted
state changes commit before optional derived work starts.

![Sequence diagram for allocate capacity and waitlist fairly](diagrams/sequence-l2-evnt-008.png)

### Behaviour — reconcile attendance after access loss

The interaction loads current scoped state before `ManageAttendancePolicy` enforces
`L2-EVNT-013`. Rejected decisions return without changing authoritative state; accepted
state changes commit before optional derived work starts.

![Sequence diagram for reconcile attendance after access loss](diagrams/sequence-l2-evnt-013.png)
