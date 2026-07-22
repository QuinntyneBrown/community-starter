# Controlled platform configuration

## Overview

Community Starter is a community platform divided into product and platform subsystems. The
Administration and insights subsystem owns this feature.

*controlled platform configuration* — subsystem capability that covers preserve configuration history and rollback and govern feature flags and kill switches

Community teams need bounded tools to configure participation and understand outcomes, while platform and support operators need narrowly scoped operational access. Administration shall never become an unaudited bypass around Community isolation, safety, privacy, or Account security. The platform shall validate, audit, stage, activate, reverse, and retire operational configuration and feature flags without exposing secrets or leaving ambiguous ownership.

The feature groups 2 traced behaviors behind one policy and evidence
boundary: `L2-ADMN-008` and `L2-ADMN-009`. Authoritative state commits before projections, delivery, or external work reports
success.

## Description

The repository contains specifications but no application implementation. This greenfield slice
defines the following building blocks across `Member Web Application`, `Community API`, the
application and domain layer, and infrastructure.

- **`ControlPlatformConfigurationSurface`** — page component in `Member Web Application`. It presents current
  state, submits user intent, and reconciles the typed result.
- **`ControlPlatformConfigurationClient`** — typed Angular client. It creates `ControlPlatformConfigurationRequest` values and maps stable
  transport failures into feature results.
- **`ControlPlatformConfigurationEndpoint`** — HTTP endpoint in `Community API`. It authenticates the
  caller, applies boundary policy, and dispatches the request.
- **`ControlPlatformConfigurationRequest`** — immutable request carrying `SubjectId`, `Action`, `ExpectedVersion`, and the
  scoped input needed by one traced behavior.
- **`ControlPlatformConfigurationHandler`** — application service that loads authorized state through
  `IControlPlatformConfigurationRepository`, invokes `ControlPlatformConfigurationPolicy`, and commits an accepted transition.
- **`ControlPlatformConfigurationPolicy`** — domain policy that evaluates current state and returns a typed
  `ControlPlatformConfigurationDecision` without performing external work.
- **`ControlPlatformConfigurationRecord`** — authoritative record containing the feature state, scope, and concurrency
  version.
- **`IControlPlatformConfigurationRepository`** — persistence port that loads scoped state and commits one conditional
  unit of work.
- **`ControlPlatformConfigurationProjector`** — idempotent post-commit component in `Community Job Worker`. It updates
  eligible projections and invokes configured external providers.

`ControlPlatformConfigurationPolicy` exposes one named operation for each traced behavior:

- **`ControlPlatformConfigurationPolicy.PreserveConfigurationHistoryAndRollback(record, request)`** — evaluates `L2-ADMN-008` (preserve configuration history and rollback) and returns a typed decision before any state change.
- **`ControlPlatformConfigurationPolicy.GovernFeatureFlagsAndKillSwitches(record, request)`** — evaluates `L2-ADMN-009` (govern feature flags and kill switches) and returns a typed decision before any state change.

## Requirements

The feature realizes the following level-2 (L2) requirements. Each row preserves the specification
identifier, its level-1 (L1) parent, and the requirement statement verbatim.

| L2 ID | Refines (L1) | Requirement |
|-------|--------------|-------------|
| `L2-ADMN-008` | `L1-ADMN-003` | Operational configuration is schema-validated, environment-scoped, versioned, secret-safe, and changed through staged activation or an explicit new rollback version. |
| `L2-ADMN-009` | `L1-ADMN-003` | Every feature flag has a typed safe default, owner, purpose, audience, activation constraints, expiry/review date, telemetry, and removal plan; safety kill switches have tested effects. |

## Diagrams

### System context

The `Community Administrator` uses `Community Platform` for the feature. The system invokes
`Delivery and Storage Providers` only for configured external work after authoritative decisions.

![C4 system context for controlled platform configuration](diagrams/c4-context.png)

### Containers

`Member Web Application` collects intent, `Community API` applies the synchronous boundary,
and `Community Database` holds authoritative state. `Community Job Worker` handles eligible
post-commit work against `Delivery and Storage Providers`.

![C4 container view for controlled platform configuration](diagrams/c4-container.png)

### Components

Inside `Community API`, `ControlPlatformConfigurationEndpoint` dispatches `ControlPlatformConfigurationHandler`. The handler evaluates
`ControlPlatformConfigurationPolicy`, persists through `IControlPlatformConfigurationRepository`, and hands committed outcomes to
`ControlPlatformConfigurationProjector`.

![C4 component view for controlled platform configuration](diagrams/c4-component.png)

### Class structure

`ControlPlatformConfigurationHandler` depends on the immutable request, domain policy, and repository port.
`ControlPlatformConfigurationRecord` owns versioned state, while `ControlPlatformConfigurationProjector` consumes committed results.

![Class diagram for controlled platform configuration](diagrams/class-structure.png)

### Behaviour — preserve configuration history and rollback

The interaction loads current scoped state before `ControlPlatformConfigurationPolicy` enforces
`L2-ADMN-008`. Rejected decisions return without changing authoritative state; accepted
state changes commit before optional derived work starts.

![Sequence diagram for preserve configuration history and rollback](diagrams/sequence-l2-admn-008.png)

### Behaviour — govern feature flags and kill switches

The interaction loads current scoped state before `ControlPlatformConfigurationPolicy` enforces
`L2-ADMN-009`. Rejected decisions return without changing authoritative state; accepted
state changes commit before optional derived work starts.

![Sequence diagram for govern feature flags and kill switches](diagrams/sequence-l2-admn-009.png)
