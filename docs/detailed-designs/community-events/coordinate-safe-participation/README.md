# Coordinated and safe participation

## Overview

Community Starter is a community platform divided into product and platform subsystems. The
Community events subsystem owns this feature.

*coordinated and safe participation* — subsystem capability that covers notify eligible attendees of changes and report and moderate Event harm

Communities need to schedule activities, control who can discover and attend them, coordinate finite capacity, and communicate changes across time zones. Event and RSVP rules are server-owned and shall remain correct under concurrent requests, cancellation, privacy, and moderation. The platform shall provide accurate Event reminders, accessible participation states, and safety integration through Event completion and retention.

The feature groups 2 traced behaviors behind one policy and evidence
boundary: `L2-EVNT-010` and `L2-EVNT-012`. Authoritative state commits before projections, delivery, or external work reports
success.

## Description

The repository contains specifications but no application implementation. This greenfield slice
defines the following building blocks across `Member Web Application`, `Community API`, the
application and domain layer, and infrastructure.

- **`CoordinateSafeParticipationSurface`** — page component in `Member Web Application`. It presents current
  state, submits user intent, and reconciles the typed result.
- **`CoordinateSafeParticipationClient`** — typed Angular client. It creates `CoordinateSafeParticipationRequest` values and maps stable
  transport failures into feature results.
- **`CoordinateSafeParticipationEndpoint`** — HTTP endpoint in `Community API`. It authenticates the
  caller, applies boundary policy, and dispatches the request.
- **`CoordinateSafeParticipationRequest`** — immutable request carrying `SubjectId`, `Action`, `ExpectedVersion`, and the
  scoped input needed by one traced behavior.
- **`CoordinateSafeParticipationHandler`** — application service that loads authorized state through
  `ICoordinateSafeParticipationRepository`, invokes `CoordinateSafeParticipationPolicy`, and commits an accepted transition.
- **`CoordinateSafeParticipationPolicy`** — domain policy that evaluates current state and returns a typed
  `CoordinateSafeParticipationDecision` without performing external work.
- **`CoordinateSafeParticipationRecord`** — authoritative record containing the feature state, scope, and concurrency
  version.
- **`ICoordinateSafeParticipationRepository`** — persistence port that loads scoped state and commits one conditional
  unit of work.
- **`CoordinateSafeParticipationProjector`** — idempotent post-commit component in `Community Job Worker`. It updates
  eligible projections and invokes configured external providers.

`CoordinateSafeParticipationPolicy` exposes one named operation for each traced behavior:

- **`CoordinateSafeParticipationPolicy.NotifyEligibleAttendeesOfChanges(record, request)`** — evaluates `L2-EVNT-010` (notify eligible attendees of changes) and returns a typed decision before any state change.
- **`CoordinateSafeParticipationPolicy.ReportAndModerateEventHarm(record, request)`** — evaluates `L2-EVNT-012` (report and moderate Event harm) and returns a typed decision before any state change.

## Requirements

The feature realizes the following level-2 (L2) requirements. Each row preserves the specification
identifier, its level-1 (L1) parent, and the requirement statement verbatim.

| L2 ID | Refines (L1) | Requirement |
|-------|--------------|-------------|
| `L2-EVNT-010` | `L1-EVNT-004` | Material changes, cancellation, waitlist promotion, and configured reminders create Notifications from committed state with current recipient, preference, and access checks. |
| `L2-EVNT-012` | `L1-EVNT-004` | Eligible people can Report an Event, organizer, or Event conduct, and authorized Moderation Actions reconcile Event visibility, attendance, location access, Deliveries, exports, and evidence. |

## Diagrams

### System context

The `Event Organizer` uses `Community Platform` for the feature. The system invokes
`Delivery and Storage Providers` only for configured external work after authoritative decisions.

![C4 system context for coordinated and safe participation](diagrams/c4-context.png)

### Containers

`Member Web Application` collects intent, `Community API` applies the synchronous boundary,
and `Community Database` holds authoritative state. `Community Job Worker` handles eligible
post-commit work against `Delivery and Storage Providers`.

![C4 container view for coordinated and safe participation](diagrams/c4-container.png)

### Components

Inside `Community API`, `CoordinateSafeParticipationEndpoint` dispatches `CoordinateSafeParticipationHandler`. The handler evaluates
`CoordinateSafeParticipationPolicy`, persists through `ICoordinateSafeParticipationRepository`, and hands committed outcomes to
`CoordinateSafeParticipationProjector`.

![C4 component view for coordinated and safe participation](diagrams/c4-component.png)

### Class structure

`CoordinateSafeParticipationHandler` depends on the immutable request, domain policy, and repository port.
`CoordinateSafeParticipationRecord` owns versioned state, while `CoordinateSafeParticipationProjector` consumes committed results.

![Class diagram for coordinated and safe participation](diagrams/class-structure.png)

### Behaviour — notify eligible attendees of changes

The interaction loads current scoped state before `CoordinateSafeParticipationPolicy` enforces
`L2-EVNT-010`. Rejected decisions return without changing authoritative state; accepted
state changes commit before optional derived work starts.

![Sequence diagram for notify eligible attendees of changes](diagrams/sequence-l2-evnt-010.png)

### Behaviour — report and moderate Event harm

The interaction loads current scoped state before `CoordinateSafeParticipationPolicy` enforces
`L2-EVNT-012`. Rejected decisions return without changing authoritative state; accepted
state changes commit before optional derived work starts.

![Sequence diagram for report and moderate Event harm](diagrams/sequence-l2-evnt-012.png)
