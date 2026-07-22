# Complete resumable onboarding

## Overview

Community Starter is a community platform divided into product and platform subsystems. The
Onboarding and discovery subsystem owns this feature.

*complete resumable onboarding* — subsystem capability that covers start and resume onboarding, complete required steps, and continue from an invitation

Onboarding moves an eligible Account from first access to a meaningful, understandable Community experience. Discovery helps the Account choose an eligible Community without exposing private Communities, blocked relationships, sensitive inference, or fabricated popularity. The platform shall present, save, validate, skip where optional, and resume onboarding steps while keeping required eligibility and policy decisions server-owned.

The feature groups 3 traced behaviors behind one policy and evidence
boundary: `L2-ONBD-001`, `L2-ONBD-002`, and `L2-ONBD-003`. Authoritative state commits before projections, delivery, or external work reports
success.

## Description

The repository contains specifications but no application implementation. This greenfield slice
defines the following building blocks across `Member Web Application`, `Community API`, the
application and domain layer, and infrastructure.

- **`CompleteResumableOnboardingSurface`** — page component in `Member Web Application`. It presents current
  state, submits user intent, and reconciles the typed result.
- **`CompleteResumableOnboardingClient`** — typed Angular client. It creates `CompleteResumableOnboardingRequest` values and maps stable
  transport failures into feature results.
- **`CompleteResumableOnboardingEndpoint`** — HTTP endpoint in `Community API`. It authenticates the
  caller, applies boundary policy, and dispatches the request.
- **`CompleteResumableOnboardingRequest`** — immutable request carrying `SubjectId`, `Action`, `ExpectedVersion`, and the
  scoped input needed by one traced behavior.
- **`CompleteResumableOnboardingHandler`** — application service that loads authorized state through
  `ICompleteResumableOnboardingRepository`, invokes `CompleteResumableOnboardingPolicy`, and commits an accepted transition.
- **`CompleteResumableOnboardingPolicy`** — domain policy that evaluates current state and returns a typed
  `CompleteResumableOnboardingDecision` without performing external work.
- **`CompleteResumableOnboardingRecord`** — authoritative record containing the feature state, scope, and concurrency
  version.
- **`ICompleteResumableOnboardingRepository`** — persistence port that loads scoped state and commits one conditional
  unit of work.
- **`CompleteResumableOnboardingProjector`** — idempotent post-commit component in `Community Job Worker`. It updates
  eligible projections and invokes configured external providers.

`CompleteResumableOnboardingPolicy` exposes one named operation for each traced behavior:

- **`CompleteResumableOnboardingPolicy.StartAndResumeOnboarding(record, request)`** — evaluates `L2-ONBD-001` (start and resume onboarding) and returns a typed decision before any state change.
- **`CompleteResumableOnboardingPolicy.CompleteRequiredSteps(record, request)`** — evaluates `L2-ONBD-002` (complete required steps) and returns a typed decision before any state change.
- **`CompleteResumableOnboardingPolicy.ContinueFromAnInvitation(record, request)`** — evaluates `L2-ONBD-003` (continue from an invitation) and returns a typed decision before any state change.

## Requirements

The feature realizes the following level-2 (L2) requirements. Each row preserves the specification
identifier, its level-1 (L1) parent, and the requirement statement verbatim.

| L2 ID | Refines (L1) | Requirement |
|-------|--------------|-------------|
| `L2-ONBD-001` | `L1-ONBD-001` | The server maintains one versioned onboarding state per Account so progress can resume across sessions and devices without trusting client-completed flags. |
| `L2-ONBD-002` | `L1-ONBD-001` | Onboarding completes only when the server verifies every currently required Account, Profile, eligibility, and policy condition; optional discovery choices do not become hidden requirements. |
| `L2-ONBD-003` | `L1-ONBD-001` | A Community invitation survives necessary sign-in, registration, verification, and onboarding steps without disclosing private Community details before the intended Account is authorized. |

## Diagrams

### System context

The `New Account Holder` uses `Community Platform` for the feature. The system invokes
`Notification Delivery` only for configured external work after authoritative decisions.

![C4 system context for complete resumable onboarding](diagrams/c4-context.png)

### Containers

`Member Web Application` collects intent, `Community API` applies the synchronous boundary,
and `Community Database` holds authoritative state. `Community Job Worker` handles eligible
post-commit work against `Notification Delivery`.

![C4 container view for complete resumable onboarding](diagrams/c4-container.png)

### Components

Inside `Community API`, `CompleteResumableOnboardingEndpoint` dispatches `CompleteResumableOnboardingHandler`. The handler evaluates
`CompleteResumableOnboardingPolicy`, persists through `ICompleteResumableOnboardingRepository`, and hands committed outcomes to
`CompleteResumableOnboardingProjector`.

![C4 component view for complete resumable onboarding](diagrams/c4-component.png)

### Class structure

`CompleteResumableOnboardingHandler` depends on the immutable request, domain policy, and repository port.
`CompleteResumableOnboardingRecord` owns versioned state, while `CompleteResumableOnboardingProjector` consumes committed results.

![Class diagram for complete resumable onboarding](diagrams/class-structure.png)

### Behaviour — start and resume onboarding

The interaction loads current scoped state before `CompleteResumableOnboardingPolicy` enforces
`L2-ONBD-001`. Rejected decisions return without changing authoritative state; accepted
state changes commit before optional derived work starts.

![Sequence diagram for start and resume onboarding](diagrams/sequence-l2-onbd-001.png)

### Behaviour — complete required steps

The interaction loads current scoped state before `CompleteResumableOnboardingPolicy` enforces
`L2-ONBD-002`. Rejected decisions return without changing authoritative state; accepted
state changes commit before optional derived work starts.

![Sequence diagram for complete required steps](diagrams/sequence-l2-onbd-002.png)

### Behaviour — continue from an invitation

The interaction loads current scoped state before `CompleteResumableOnboardingPolicy` enforces
`L2-ONBD-003`. Rejected decisions return without changing authoritative state; accepted
state changes commit before optional derived work starts.

![Sequence diagram for continue from an invitation](diagrams/sequence-l2-onbd-003.png)
