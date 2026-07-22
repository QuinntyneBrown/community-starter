# Safe templates and operable channels

## Overview

Community Starter is a community platform divided into product and platform subsystems. The
Notifications and delivery subsystem owns this feature.

*safe templates and operable channels* — subsystem capability that covers render safe templates and action links, observe and repair Delivery health, and disable unconfigured channels cleanly

Accounts need timely, understandable notice of committed activity without receiving content they can no longer access or channels they declined. A Notification is durable Account-facing state; a Notification Delivery is an idempotent attempt through a configured external channel and may fail independently. The platform shall secure Notification links and templates, expose privacy-safe delivery health, and keep unconfigured or disabled channels inert.

The feature groups 3 traced behaviors behind one policy and evidence
boundary: `L2-NOTF-010`, `L2-NOTF-011`, and `L2-NOTF-012`. Authoritative state commits before projections, delivery, or external work reports
success.

## Description

The repository contains specifications but no application implementation. This greenfield slice
defines the following building blocks across `Member Web Application`, `Community API`, the
application and domain layer, and infrastructure.

- **`OperateNotificationChannelsSurface`** — page component in `Member Web Application`. It presents current
  state, submits user intent, and reconciles the typed result.
- **`OperateNotificationChannelsClient`** — typed Angular client. It creates `OperateNotificationChannelsRequest` values and maps stable
  transport failures into feature results.
- **`OperateNotificationChannelsEndpoint`** — HTTP endpoint in `Community API`. It authenticates the
  caller, applies boundary policy, and dispatches the request.
- **`OperateNotificationChannelsRequest`** — immutable request carrying `SubjectId`, `Action`, `ExpectedVersion`, and the
  scoped input needed by one traced behavior.
- **`OperateNotificationChannelsHandler`** — application service that loads authorized state through
  `IOperateNotificationChannelsRepository`, invokes `OperateNotificationChannelsPolicy`, and commits an accepted transition.
- **`OperateNotificationChannelsPolicy`** — domain policy that evaluates current state and returns a typed
  `OperateNotificationChannelsDecision` without performing external work.
- **`OperateNotificationChannelsRecord`** — authoritative record containing the feature state, scope, and concurrency
  version.
- **`IOperateNotificationChannelsRepository`** — persistence port that loads scoped state and commits one conditional
  unit of work.
- **`OperateNotificationChannelsProjector`** — idempotent post-commit component in `Community Job Worker`. It updates
  eligible projections and invokes configured external providers.

`OperateNotificationChannelsPolicy` exposes one named operation for each traced behavior:

- **`OperateNotificationChannelsPolicy.RenderSafeTemplatesAndActionLinks(record, request)`** — evaluates `L2-NOTF-010` (render safe templates and action links) and returns a typed decision before any state change.
- **`OperateNotificationChannelsPolicy.ObserveAndRepairDeliveryHealth(record, request)`** — evaluates `L2-NOTF-011` (observe and repair Delivery health) and returns a typed decision before any state change.
- **`OperateNotificationChannelsPolicy.DisableUnconfiguredChannelsCleanly(record, request)`** — evaluates `L2-NOTF-012` (disable unconfigured channels cleanly) and returns a typed decision before any state change.

## Requirements

The feature realizes the following level-2 (L2) requirements. Each row preserves the specification
identifier, its level-1 (L1) parent, and the requirement statement verbatim.

| L2 ID | Refines (L1) | Requirement |
|-------|--------------|-------------|
| `L2-NOTF-010` | `L1-NOTF-004` | Versioned templates accept only declared encoded variables and produce canonical action links that cannot become open redirects, reusable secrets, or accidental disclosures. |
| `L2-NOTF-011` | `L1-NOTF-004` | Operators can detect queue age, latency, failure, suppression, provider degradation, and template errors through privacy-safe metrics, traces, alerts, and runbooks. |
| `L2-NOTF-012` | `L1-NOTF-004` | An external channel remains inert until its provider, sender identity, callback verification, templates, privacy purpose, operational ownership, and ambiguous-outcome safety pass startup and deployment validation. |

## Diagrams

### System context

The `Account Holder` uses `Community Platform` for the feature. The system invokes
`Email Delivery Provider` only for configured external work after authoritative decisions.

![C4 system context for safe templates and operable channels](diagrams/c4-context.png)

### Containers

`Member Web Application` collects intent, `Community API` applies the synchronous boundary,
and `Community Database` holds authoritative state. `Community Job Worker` handles eligible
post-commit work against `Email Delivery Provider`.

![C4 container view for safe templates and operable channels](diagrams/c4-container.png)

### Components

Inside `Community API`, `OperateNotificationChannelsEndpoint` dispatches `OperateNotificationChannelsHandler`. The handler evaluates
`OperateNotificationChannelsPolicy`, persists through `IOperateNotificationChannelsRepository`, and hands committed outcomes to
`OperateNotificationChannelsProjector`.

![C4 component view for safe templates and operable channels](diagrams/c4-component.png)

### Class structure

`OperateNotificationChannelsHandler` depends on the immutable request, domain policy, and repository port.
`OperateNotificationChannelsRecord` owns versioned state, while `OperateNotificationChannelsProjector` consumes committed results.

![Class diagram for safe templates and operable channels](diagrams/class-structure.png)

### Behaviour — render safe templates and action links

The interaction loads current scoped state before `OperateNotificationChannelsPolicy` enforces
`L2-NOTF-010`. Rejected decisions return without changing authoritative state; accepted
state changes commit before optional derived work starts.

![Sequence diagram for render safe templates and action links](diagrams/sequence-l2-notf-010.png)

### Behaviour — observe and repair Delivery health

The interaction loads current scoped state before `OperateNotificationChannelsPolicy` enforces
`L2-NOTF-011`. Rejected decisions return without changing authoritative state; accepted
state changes commit before optional derived work starts.

![Sequence diagram for observe and repair Delivery health](diagrams/sequence-l2-notf-011.png)

### Behaviour — disable unconfigured channels cleanly

The interaction loads current scoped state before `OperateNotificationChannelsPolicy` enforces
`L2-NOTF-012`. Rejected decisions return without changing authoritative state; accepted
state changes commit before optional derived work starts.

![Sequence diagram for disable unconfigured channels cleanly](diagrams/sequence-l2-notf-012.png)
