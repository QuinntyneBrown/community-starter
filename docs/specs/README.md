# Community platform MVP requirements

**Status:** Draft

**Last reviewed:** 2026-07-21

**Requirements owner:** Product Owner; a named accountable person must be assigned before any
requirement moves from `proposed` to `accepted`.

## Purpose

This branch defines the smallest production-launchable community platform selected from the full
feature catalog. It contains 25 end-to-end feature slices and only the product and platform
subsystems required to deliver those slices safely.

The MVP is not a mock or client-only prototype. Each applicable slice traverses the Angular UI,
typed client, HTTP contract, Application use case, Domain policy, relational persistence,
post-commit processing, and observable deployed behavior. A requirement remains `proposed` until it
is accepted, implemented, and backed by the verification evidence it declares.

## MVP principles

- Server state, current authorization, Community scope, Blocks, Moderation Actions, and privacy
  policy govern every protected read and mutation.
- State is persisted before Feed, Search, realtime, Notification, or other derived effects claim
  success.
- External work is durable, bounded, retry-safe, and reconciled from authoritative state.
- Accessibility, security, privacy, recovery, and operational evidence are part of the MVP rather
  than post-launch enhancements.
- Stable requirement identifiers are preserved from the full catalog. Numbering gaps identify
  deliberately deferred behavior and must not be closed by renumbering retained requirements.

## The 25 MVP feature slices

| # | Feature | Involved subsystems |
| ---: | --- | --- |
| 1 | Public landing and Community preview | Public Web; Onboarding; Communities; Experience System |
| 2 | Account registration and verification | Identity; Notification Delivery; Security |
| 3 | Sign-in, recovery, and Session management | Identity; Security; Administration Audit |
| 4 | Resumable onboarding | Onboarding; Identity; Communities; Profiles; Feeds |
| 5 | Community creation and settings | Communities; Authorization; Administration Audit |
| 6 | Open join, gated requests, and invitations | Memberships; Onboarding; Notifications |
| 7 | Roles and Permissions | Communities; Identity Authorization; Administration Audit |
| 8 | Leave, remove, suspend, and rejoin | Memberships; Privacy Lifecycle; Safety; Notifications |
| 9 | Spaces and participation rules | Communities; Content; Discovery; Events; Messaging |
| 10 | Profiles and Member Directory | Profiles; Memberships; Privacy; Media; Search |
| 11 | Block and mute controls | Relationships; Discovery; Messaging; Notifications; Safety |
| 12 | Post creation, editing, and removal | Content; Authorization; Feeds; Search; Notifications |
| 13 | Safe media Attachments | Content Media; Storage; Jobs; Security; Privacy |
| 14 | Comments, Mentions, and safe rendering | Content; Profiles; Notifications; Safety |
| 15 | Account and Community Feeds | Discovery; Content; Events; Authorization; Projections |
| 16 | Search and Tags | Discovery; Content; Profiles; Communities; Authorization |
| 17 | Reactions and Bookmarks | Engagement; Content; Authorization; Abuse Controls |
| 18 | Consent-based direct Messaging | Messaging; Realtime; Relationships; Notifications; Retention |
| 19 | Basic Events, capacity, and RSVP | Events; Communities; Feeds; Notifications; Privacy |
| 20 | Notification inbox, preferences, and email Delivery | Notifications; Templates; Destinations; Jobs; Privacy |
| 21 | User reporting | Safety; Content; Messaging; Events; Evidence |
| 22 | Moderation Cases, Actions, and Appeals | Safety; Authorization; Audit; Projections; Notifications |
| 23 | Community administration and audit | Administration; Communities; Authorization; Support Cases |
| 24 | Account privacy and data lifecycle | Privacy; Identity; Profiles; Retention; Export; Deletion Jobs |
| 25 | Production delivery and operations | Architecture; Security; Operations; Quality; Experience; Starter Tooling |

These are vertical feature boundaries, not permission to duplicate infrastructure. Shared technical
services remain owned by one subsystem and are consumed through explicit contracts.

## Users and roles

| Role | Purpose |
| --- | --- |
| Visitor | Reads permitted public content and evaluates whether to join. |
| Account holder | Manages identity, Sessions, security, privacy, and platform preferences. |
| Member | Participates through one active Membership in a Community. |
| Community Owner | Retains ultimate responsibility and preserves an eligible administrative successor. |
| Community Administrator | Configures a Community and delegates Roles within current authority. |
| Moderator | Reviews Reports and applies bounded, auditable Moderation Actions. |
| Event Organizer | Creates and manages eligible Events and attendance. |
| Platform Operator | Operates configuration, availability, recovery, and safety boundaries. |
| Support Operator | Resolves Support Cases with purpose-bound, audited access. |
| Service Identity | Performs one declared machine operation within an explicit scope. |

A person can hold several roles. Every authorization decision uses the current Account, Community,
Membership, Role, resource, and requested action; routes and displayed controls never establish
authority.

## Coverage register

| Code | MVP subsystem | Specification | L1 | L2 | Status | Evidence |
| --- | --- | --- | ---: | ---: | --- | --- |
| AUTH | Identity and access | [L1](identity-access/L1.md) · [L2](identity-access/L2.md) | 4 | 15 | proposed | — |
| COMM | Communities and Memberships | [L1](communities-membership/L1.md) · [L2](communities-membership/L2.md) | 5 | 16 | proposed | — |
| PROF | Profiles and relationships | [L1](profiles-relationships/L1.md) · [L2](profiles-relationships/L2.md) | 3 | 11 | proposed | — |
| ONBD | Onboarding and discovery | [L1](onboarding-discovery/L1.md) · [L2](onboarding-discovery/L2.md) | 4 | 10 | proposed | — |
| CONT | Content and media | [L1](content-media/L1.md) · [L2](content-media/L2.md) | 4 | 14 | proposed | — |
| DISC | Feeds, Search, and engagement | [L1](feeds-search-engagement/L1.md) · [L2](feeds-search-engagement/L2.md) | 4 | 12 | proposed | — |
| MESS | Direct Messaging and realtime | [L1](messaging-realtime/L1.md) · [L2](messaging-realtime/L2.md) | 3 | 10 | proposed | — |
| NOTF | Notifications and email Delivery | [L1](notifications-delivery/L1.md) · [L2](notifications-delivery/L2.md) | 4 | 12 | proposed | — |
| EVNT | Basic Community Events | [L1](community-events/L1.md) · [L2](community-events/L2.md) | 4 | 10 | proposed | — |
| SAFE | Moderation, trust, and safety | [L1](moderation-trust-safety/L1.md) · [L2](moderation-trust-safety/L2.md) | 4 | 15 | proposed | — |
| ADMN | Administration and audit | [L1](administration-insights/L1.md) · [L2](administration-insights/L2.md) | 4 | 11 | proposed | — |
| PRIV | Privacy and data lifecycle | [L1](privacy-data-lifecycle/L1.md) · [L2](privacy-data-lifecycle/L2.md) | 4 | 15 | proposed | — |
| ARCH | Platform architecture and data | [L1](platform-architecture/L1.md) · [L2](platform-architecture/L2.md) | 6 | 16 | proposed | — |
| UXDS | Experience and design system | [L1](experience-design-system/L1.md) · [L2](experience-design-system/L2.md) | 5 | 14 | proposed | — |
| MKTG | Marketing and public web | [L1](marketing-public-web/L1.md) · [L2](marketing-public-web/L2.md) | 4 | 12 | proposed | — |
| SECU | Platform security | [L1](security/L1.md) · [L2](security/L2.md) | 5 | 17 | proposed | — |
| OPER | Operations and reliability | [L1](operations-reliability/L1.md) · [L2](operations-reliability/L2.md) | 5 | 15 | proposed | — |
| QUAL | Delivery and quality | [L1](delivery-quality/L1.md) · [L2](delivery-quality/L2.md) | 6 | 22 | proposed | — |
| STRT | Starter adoption and developer experience | [L1](starter-experience/L1.md) · [L2](starter-experience/L2.md) | 4 | 13 | proposed | — |

The MVP register contains 82 L1 outcomes and 260 L2 behaviors across 19 subsystems. Every retained
L2 traces to exactly one retained L1. All requirements are `proposed`, and evidence remains empty
until implementation and appropriately scoped verification exist.

## Deliberately deferred scope

The MVP contains no requirement catalog for the following capabilities. Their absence is deliberate
and the product must not expose controls, routes, workers, credentials, or claims that imply they are
available:

- Billing, paid Plans, Subscriptions, commercial Entitlements, and payment-provider processing.
- Public integration credentials, Webhook Endpoints, webhook Delivery, and bulk imports.
- External identity linking and a Follow graph.
- Community retirement and its multi-resource terminal workflow.
- Recommendation models or recommendation-signal controls.
- Content link previews.
- Group Conversations and Message Attachments.
- Recurring Events, check-in or roster export, calendar export, and targeted Event invitations.
- Notification digests, quiet-time scheduling, push, and SMS; MVP external Delivery is email only.
- Automated moderation decisions or emergency automation.
- Support impersonation, break-glass access, insight export, and Support Case Attachments.
- Age- or guardian-specific participation flows pending a product and privacy decision.
- Split-origin deployment, full bidirectional-language support, advanced artifact attestation,
  automated drift management, and horizontal-scaling claims.

Deferred behavior requires a later accepted product decision and restoration or replacement of its
stable requirements before implementation.

## Specification conventions

- Requirements are grouped by subsystem with separate `L1.md` outcome and `L2.md` behavior files.
- L1 identifiers use `L1-{CODE}-{NNN}`. L2 identifiers use `L2-{CODE}-{NNN}` and trace to exactly one
  L1 outcome.
- Stable IDs are not renumbered in this subset. A gap means that behavior is outside MVP scope.
- `proposed` means drafted but not accepted. `implemented` requires evidence at every verification
  layer named by the requirement.
- Acceptance criteria are numbered Given/When/Then statements.
- `Related: —` and `Evidence: —` mean the artifact does not exist yet, not that it is unnecessary.
- A fake backend, mock, unit test, or successful build cannot alone prove cross-stack behavior.

## MVP completion gates

The MVP is complete only when:

1. Every selected public and protected journey has loading, empty, success, validation,
   authorization, conflict/error, destructive, narrow-screen, keyboard, announcement, and
   reduced-motion behavior where applicable.
2. Server-owned invariants have Domain evidence, public boundaries have integration/contract
   evidence, components have focused evidence, and critical journeys have live full-stack evidence.
3. Community scoping, Blocks, Moderation Actions, retention, and deletion are tested across reads,
   writes, Feed, Search, realtime, Jobs, caches, blobs, and Notification Delivery.
4. The immutable release candidate passes routing, asset, header, migration, health,
   accessibility, and critical-API smoke checks in a production-like environment.
5. Security, privacy, accessibility, recovery, load, licensing, and operational reviews record
   scope, date, findings, ownership, and unresolved risk before a production-ready claim.

## First implementation slice

The first slice proves safe participation across the whole technical spine:

1. A Visitor registers and verifies an Account.
2. An owner creates a Community and invites a second Account.
3. The invitee accepts one Membership and receives server-computed Permissions.
4. The Member publishes one visible Post; another eligible Member sees and reacts to it.
5. A disallowed Account cannot read or mutate the Post through any identifier or projection.
6. The Post is reported, reviewed, and hidden by a Moderator; direct URL, Feed, Search,
   Notification, and realtime state reconcile to the committed Action.

The slice requires Domain, integration, component, live journey, accessibility, and published-
artifact evidence before any involved L2 is marked `implemented`.
