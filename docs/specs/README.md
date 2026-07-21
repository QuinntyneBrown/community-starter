# Community platform starter requirements

**Status:** Draft

**Last reviewed:** 2026-07-21

**Requirements owner:** Product Owner; a named accountable person must be assigned before any
requirement moves from `proposed` to `accepted`.

## Purpose

These specifications define a complete, adaptable starting point for a production-scale community
platform. The starter serves public visitors, members, community teams, platform operators, support
staff, and automated service identities across web, API, background-processing, and operational
surfaces.

The draft describes required outcomes and falsifiable behavior. It does not claim that those
outcomes are implemented or independently reviewed. A deployment may be called production-ready
only after the applicable requirements are accepted, implemented, traced to evidence, and pass the
readiness reviews defined here.

## Product thesis

For organizations and organizers who need a safe, durable place for people to gather, publish,
converse, and coordinate, the starter enables teams to launch a branded, multi-community platform
without rebuilding identity, Community isolation, moderation, privacy, delivery, and operations from scratch.
Unlike a collection of generated CRUD screens, it provides server-authoritative participation and
safety rules, a coherent member journey, explicit platform boundaries, and production evidence
requirements that can be specialized without weakening the baseline.

## Success measures and ownership

These proposed starter-level measures define whether specialization is producing a usable and
defensible product. A specialization may make a target stricter, but must record an owner and the
evidence profile before acceptance.

| Measure | Proposed target | Required evidence | Accountable owner |
| --- | --- | --- | --- |
| Safe first participation | At least 90% of representative usability participants complete sign-up, Community join, and first permitted Post without facilitator intervention; no participant completes a forbidden read or mutation. | Moderated usability study plus live authorization journey against the published artifact. | Product Owner |
| Isolation and safety correctness | Zero unauthorized disclosures or prohibited effects in the accepted Community, Block, Moderation Action, Entitlement, retention, and deletion test matrix. | Domain, integration, adversarial, and live cross-projection evidence. | Security Owner |
| Critical-journey accessibility | Every critical journey has no unresolved critical or serious finding in the accepted automated and manual accessibility review. | Automated scan, keyboard and screen-reader review, zoom/reflow, contrast, motion, localization, and media evidence. | Accessibility Owner |
| Moderation effect propagation | 100% of sampled committed restrictive Actions reach every applicable request, cache, discovery, Delivery, export, and realtime surface within the accepted effect objective. | Timestamp-correlated live journeys, projection metrics, reconciliation results, and exception review. | Trust and Safety Owner |
| Release confidence | 100% of applicable release-blocking gates pass against the immutable candidate artifact; no production-ready claim relies only on mocks or development servers. | CI results, artifact manifest, staging smoke, recovery exercise, and signed review record. | Release Owner |
| Starter adoption | A new supported workstation reaches the documented first vertical slice locally within 60 minutes at the 90th percentile, excluding prerequisite downloads. | Timed clean-machine study and quickstart telemetry or observation with privacy review. | Developer Experience Owner |

## Users and roles

| Role | Purpose |
| --- | --- |
| Visitor | Reads permitted public content and evaluates whether to join. |
| Account holder | Manages the identity, sessions, security, and platform-level preferences of one person. |
| Member | Participates through an active Membership in a Community. |
| Community Owner | Holds ultimate responsibility for a Community, including ownership transfer, archive, or retirement. |
| Community Administrator | Configures a Community and delegates Roles within granted authority. |
| Moderator | Reviews Reports and applies bounded Moderation Actions. |
| Event Organizer | Creates and manages Events when granted the corresponding Permission. |
| Platform Operator | Operates the shared platform, its policies, availability, and Community lifecycle. |
| Support Operator | Resolves Support Cases using time-bound, audited, least-privilege access. |
| Service Identity | Performs a declared machine-to-machine operation within an explicit scope. |

A person can hold several roles. Every authorization decision uses the current Account, Community,
Membership, Role, resource, and action; a displayed control or client route is never proof of
permission.

## Canonical vocabulary

| Product noun | Definition | UI synonym, if any | Not to be confused with |
| --- | --- | --- | --- |
| Account | Platform identity and security boundary for one person. | Account | Profile or Membership |
| Session | Revocable authenticated interaction issued to an Account and client. | Signed-in device | Account |
| Community | Isolation and policy boundary in which members participate. | Group, hub | The whole platform |
| Space | Optional named subdivision of one Community for scoped content and participation. | Channel | Community |
| Membership | Time-bounded relationship between an Account and one Community. | Member record | Account |
| Role | Named, Community-scoped collection of Permissions. | Team role | Platform operator access |
| Permission | Server-evaluated authority to perform one action in one scope. | Access | UI visibility |
| Profile | Member-facing identity presentation and privacy-controlled attributes. | Member profile | Account credentials |
| Member Directory | Community-scoped, privacy-filtered listing of eligible Membership/Profile context. | Members | Platform Account list |
| Follow | A one-way request to prioritize another permitted Profile's activity. | Follow | Membership |
| Block | A safety boundary that prevents defined discovery, interaction, and delivery paths. | Block | Moderation Action |
| Mute | A private preference that suppresses defined content or notifications without notifying its subject. | Hide | Block |
| Post | Primary authored content published to a Community or Space. | Topic, thread | Comment |
| Comment | Authored response attached to a Post or supported parent Comment. | Reply | Message |
| Attachment | Stored media or file associated with content, a Message, or a Support Case and governed by that parent's access and lifecycle boundary. | Upload | External link |
| Reaction | One Account's selected response of an allowed type to a content item. | Like | Comment |
| Bookmark | Private saved-content reference owned by an Account. | Save | Reaction |
| Tag | Community-governed label used to classify and discover content. | Topic label | Space |
| Mention | Stable reference from authored content to one eligible Profile. | @mention | Typed display text |
| Quote Reference | Stable reference from authored content to an eligible Post or Comment. | Quote | Copied text |
| Feed | Ordered, cursor-paginated projection of Posts and individual Event occurrences an Account may currently view. | Timeline | Search results or an Event series |
| Conversation | Membership-controlled container for private Messages. | Chat | Comment thread |
| Message | Authored item delivered within a Conversation. | Chat message | Notification |
| Event | Scheduled Community activity with attendance rules and lifecycle. | Meetup | Post |
| RSVP | A Member's current attendance response to an Event. | Attendance | Membership |
| Notification | Account-facing record that a relevant event occurred. | Alert | Delivery attempt |
| Delivery | Durable attempt to send a Notification or integration event through a configured external destination. | Send attempt | Notification or source event |
| Notification Delivery | Delivery of a Notification through email, push, SMS, or another configured Account channel. | Email, push | Notification |
| Webhook Delivery | Delivery of a signed integration event to a Webhook Endpoint. | Webhook attempt | Provider callback |
| Report | A person's allegation that content, conduct, or an Account violates policy. | Flag | Moderation Case |
| Report Receipt | Account-bound proof of a prior authorized view and protected reference to its exact reportable version. | Recent-view proof | Report acknowledgement |
| Moderation Case | Durable triage record joining Reports, evidence, decisions, and review history. | Case | Support Case |
| Support Case | Durable operational record for a person's support request, authority, work, communication, and closure. | Support ticket | Moderation Case |
| Moderation Action | Bounded policy enforcement against content, Membership, or Account. | Sanction | Block |
| Appeal | Request to review an eligible Moderation Action. | Review request | Report |
| Plan | Commercial definition of prices, limits, and included Entitlements. | Tier | Subscription |
| Subscription | A billing relationship that follows a defined lifecycle. | Membership plan | Community Membership |
| Subscription Class | Server-defined mutually exclusive product family in which one billing subject may hold at most one active Subscription. | Product family | Plan |
| Entitlement | Server-evaluated commercial capability or limit available to a subject. | Feature access | Permission |
| Audit Event | Immutable-enough security or administrative record of actor, action, target, and outcome. | Audit log entry | Analytics event |
| Job | Durable unit of asynchronous work with retry and terminal-failure semantics. | Background task | Request |
| Webhook Endpoint | Registered destination for signed integration deliveries. | Webhook | API endpoint |
| Public Projection | Anonymous, allow-listed HTML representation of an explicitly public Community, Profile, Post, or Event. | Public page | Authenticated application route |

## Primary journey

1. A Visitor reaches fast, crawlable marketing or public Community content and chooses a clear
   sign-up, sign-in, or join action.
2. The person creates or authenticates an Account, verifies required contact and security factors,
   and receives revocable Session state.
3. The Account creates, discovers, is invited to, or requests access to a Community. The server
   establishes an allowed Membership, Role, and effective Permissions.
4. The Member completes a Profile, enters a permitted Space, publishes a Post or Event, and sees the
   committed result in a deterministic Feed and Search when eligible.
5. Other Members react, comment, follow, message, RSVP, or manage Notification preferences. Blocks, mutes, visibility,
   Membership state, policy, and commercial Entitlements are enforced on every server path.
6. Notifications and realtime hints follow successful persistence; clients reconcile from the API
   and background Deliveries remain retryable and idempotent.
7. A Member can Report harm. A Moderator triages a Moderation Case, preserves authorized evidence,
   applies an allowed Moderation Action, notifies affected people, and supports an eligible Appeal.
8. Community teams use privacy-safe insights and audited administration, while Platform Operators
   monitor service objectives, recover data, support users safely, and promote one verified artifact.

## Product-defining invariants

- The server must establish Community scope and effective Permission for every protected read and
  mutation; identifiers, routes, cached client state, and hidden controls never grant access.
- A Membership may transition only through its defined lifecycle, and every Community must retain
  at least one eligible owner until terminal retirement; ownership transfer must preserve an eligible successor.
- Content, Events, Messages, Feeds, Search, Notifications, exports, analytics, and integrations must
  honor current visibility, Membership, Block, Moderation Action, and deletion state.
- State-changing use cases must validate, authorize, evaluate policy, persist atomically, and only
  then emit realtime or integration work. Failure must not leave a partial transition.
- Moderation decisions must be explainable and attributable: eligible actions carry actor, policy
  basis, target, timestamps, scope, reason, and appeal or expiry state in an Audit Event.
- External callbacks, Job retries, Deliveries, and billing events must be idempotent; duplicate or
  out-of-order input cannot duplicate user-visible or financial effects.
- Reversible lifecycle state such as suspended, archived, cancelled, or Case/Conversation closure
  must remain distinct from irreversible erasure.
- A fake client backend, mock, unit test, or successful build cannot by itself prove cross-stack or
  production behavior.

## Scope and specialization

The baseline includes all capabilities in the coverage register. Product adopters may disable an
optional user-facing capability such as paid Plans or direct Messages only through an explicit,
tested configuration profile that removes its routes, navigation, processing, credentials, and
claims without weakening shared security, privacy, or operational controls.

Specialization may add stricter policy, alternate presentation, or new capabilities. It may not
silently weaken Community isolation, server authority, traceability, accessibility, data lifecycle,
security, recovery, or delivery gates. Consequential changes to data ownership, public contracts,
security, deployment, licensing, or operational cost require a local architecture decision record.

### Explicit non-goals

- A single-community assumption embedded in persistence, authorization, or routing.
- Client-only authorization, moderation, quotas, workflow rules, or Entitlement enforcement.
- A native mobile application, federation protocol, marketplace, or custom recommendation model in
  the baseline; these require a product decision and their own requirements.
- A social graph or engagement mechanic that bypasses privacy and safety controls.
- Empty architecture projects, speculative shared abstractions, or placeholder production services
  that make an unimplemented capability appear ready.
- Claims of legal compliance, security review, accessibility conformance, scale, or recovery that
  are not backed by current evidence.

## Specification conventions

- Requirements are grouped by capability. Each capability has a stable code and separate `L1.md`
  outcome and `L2.md` behavior files.
- L1 identifiers use `L1-{CODE}-{NNN}`. L2 identifiers use `L2-{CODE}-{NNN}` and trace to exactly one
  L1 outcome.
- `proposed` means drafted but not accepted. `accepted` means approved as intended behavior.
  `implemented` requires evidence at every layer named by the requirement. `superseded` links to its
  replacement and remains in history.
- Sources are limited to `product brief`, `research`, `accepted mock`, `implementation`, or an
  explicit combination of those labels.
- Acceptance criteria are numbered Given/When/Then statements. Tests that prove an L2 carry a
  `Traces to: L2-...` comment.
- `Related: —` and `Evidence: —` mean the referenced artifact does not exist yet, not that evidence
  is unnecessary.
- Cross-stack behavior requires live-contract or full-stack evidence. A browser suite using a fake
  backend is useful journey evidence but is not proof of API or persistence behavior.

## Coverage register

| Code | Capability | Specification | L1 | L2 | Status | Evidence |
| --- | --- | --- | ---: | ---: | --- | --- |
| AUTH | Identity and access | [L1](identity-access/L1.md) · [L2](identity-access/L2.md) | 4 | 16 | proposed | — |
| COMM | Communities and Memberships | [L1](communities-membership/L1.md) · [L2](communities-membership/L2.md) | 5 | 17 | proposed | — |
| PROF | Profiles and relationships | [L1](profiles-relationships/L1.md) · [L2](profiles-relationships/L2.md) | 4 | 14 | proposed | — |
| ONBD | Onboarding and discovery | [L1](onboarding-discovery/L1.md) · [L2](onboarding-discovery/L2.md) | 4 | 12 | proposed | — |
| CONT | Content and media | [L1](content-media/L1.md) · [L2](content-media/L2.md) | 4 | 15 | proposed | — |
| DISC | Feeds, search, and engagement | [L1](feeds-search-engagement/L1.md) · [L2](feeds-search-engagement/L2.md) | 4 | 12 | proposed | — |
| MESS | Messaging and realtime | [L1](messaging-realtime/L1.md) · [L2](messaging-realtime/L2.md) | 4 | 16 | proposed | — |
| NOTF | Notifications and delivery | [L1](notifications-delivery/L1.md) · [L2](notifications-delivery/L2.md) | 4 | 13 | proposed | — |
| EVNT | Community events | [L1](community-events/L1.md) · [L2](community-events/L2.md) | 4 | 15 | proposed | — |
| SAFE | Moderation, trust, and safety | [L1](moderation-trust-safety/L1.md) · [L2](moderation-trust-safety/L2.md) | 4 | 16 | proposed | — |
| ADMN | Administration and insights | [L1](administration-insights/L1.md) · [L2](administration-insights/L2.md) | 4 | 15 | proposed | — |
| BILL | Billing and Entitlements | [L1](billing-entitlements/L1.md) · [L2](billing-entitlements/L2.md) | 5 | 21 | proposed | — |
| PRIV | Privacy and data lifecycle | [L1](privacy-data-lifecycle/L1.md) · [L2](privacy-data-lifecycle/L2.md) | 4 | 17 | proposed | — |
| INTG | APIs and integrations | [L1](integrations-api/L1.md) · [L2](integrations-api/L2.md) | 4 | 15 | proposed | — |
| ARCH | Platform architecture and data | [L1](platform-architecture/L1.md) · [L2](platform-architecture/L2.md) | 6 | 16 | proposed | — |
| UXDS | Experience and design system | [L1](experience-design-system/L1.md) · [L2](experience-design-system/L2.md) | 6 | 17 | proposed | — |
| MKTG | Marketing and public web | [L1](marketing-public-web/L1.md) · [L2](marketing-public-web/L2.md) | 4 | 13 | proposed | — |
| SECU | Platform security | [L1](security/L1.md) · [L2](security/L2.md) | 5 | 17 | proposed | — |
| OPER | Operations and reliability | [L1](operations-reliability/L1.md) · [L2](operations-reliability/L2.md) | 5 | 18 | proposed | — |
| QUAL | Delivery and quality | [L1](delivery-quality/L1.md) · [L2](delivery-quality/L2.md) | 6 | 23 | proposed | — |
| STRT | Starter adoption and developer experience | [L1](starter-experience/L1.md) · [L2](starter-experience/L2.md) | 4 | 15 | proposed | — |

The register currently contains 94 L1 outcomes and 333 L2 behaviors. All 333 L2 requirements are
`proposed`; their evidence cells remain empty until implementation and appropriately scoped
verification exist.

## Cross-capability completion gates

A product specialization is complete only when all applicable capability requirements and these
gates are satisfied:

1. Every public or protected user journey has loading, empty, success, validation, authorization,
   conflict/error, destructive, narrow-screen, keyboard, announcement, and reduced-motion behavior
   where applicable.
2. Every server-owned invariant has direct domain or Application evidence, every public boundary has
   integration or contract evidence, and every critical journey has live full-stack evidence.
3. Community scoping, Blocks, Moderation Actions, Entitlements, retention, and deletion have tests across
   primary reads, writes, Search, Feeds, realtime, background Jobs, caches, blobs, and integrations.
4. The published artifact passes routing, asset, header, migration, health, accessibility, and
   critical-API smoke tests in an environment configured like production.
5. Independent security, privacy, accessibility, recovery, load, licensing, and operational reviews
   record their scope, date, findings, owner, and unresolved risk before a production-ready claim.

## First vertical slice

The first implementation slice proves safe participation rather than isolated scaffolding:

1. A Visitor creates and verifies an Account.
2. An owner creates a Community and invites a second Account.
3. The invited Account accepts one Membership and receives a server-computed Role and Permissions.
4. The Member publishes one Post with a valid visibility scope; another Member sees and reacts to it.
5. A disallowed Account cannot read or mutate the Post, including through a guessed identifier.
6. The Post can be reported, reviewed, and hidden by an authorized Moderator; Feed, Search, direct URL,
   Notification, and realtime projections reconcile to the committed decision.

Outcome traces: [L1-AUTH-001](identity-access/L1.md#l1-auth-001-safe-account-lifecycle),
[L1-COMM-001](communities-membership/L1.md#l1-comm-001-govern-community-lifecycle),
[L1-COMM-002](communities-membership/L1.md#l1-comm-002-control-membership-lifecycle),
[L1-COMM-003](communities-membership/L1.md#l1-comm-003-delegate-community-authority),
[L1-CONT-001](content-media/L1.md#l1-cont-001-govern-post-lifecycle),
[L1-DISC-001](feeds-search-engagement/L1.md#l1-disc-001-deliver-useful-feeds),
[L1-DISC-002](feeds-search-engagement/L1.md#l1-disc-002-search-authorized-content), and
[L1-SAFE-003](moderation-trust-safety/L1.md#l1-safe-003-proportionate-server-owned-enforcement).

Primary behavior traces: [L2-AUTH-001](identity-access/L2.md#l2-auth-001-register-an-account-safely),
[L2-AUTH-002](identity-access/L2.md#l2-auth-002-verify-account-ownership),
[L2-AUTH-008](identity-access/L2.md#l2-auth-008-evaluate-current-permissions),
[L2-AUTH-009](identity-access/L2.md#l2-auth-009-enforce-community-boundaries),
[L2-COMM-001](communities-membership/L2.md#l2-comm-001-create-a-community),
[L2-COMM-006](communities-membership/L2.md#l2-comm-006-invite-an-account),
[L2-COMM-008](communities-membership/L2.md#l2-comm-008-manage-roles-and-permissions),
[L2-COMM-009](communities-membership/L2.md#l2-comm-009-assign-a-role-safely),
[L2-CONT-001](content-media/L2.md#l2-cont-001-draft-and-publish-a-post),
[L2-CONT-003](content-media/L2.md#l2-cont-003-enforce-post-visibility),
[L2-CONT-013](content-media/L2.md#l2-cont-013-commit-content-atomically),
[L2-DISC-001](feeds-search-engagement/L2.md#l2-disc-001-build-the-account-feed),
[L2-DISC-005](feeds-search-engagement/L2.md#l2-disc-005-search-eligible-records),
[L2-DISC-007](feeds-search-engagement/L2.md#l2-disc-007-remove-stale-search-results),
[L2-DISC-008](feeds-search-engagement/L2.md#l2-disc-008-add-and-remove-a-reaction),
[L2-SAFE-002](moderation-trust-safety/L2.md#l2-safe-002-submit-a-safe-report),
[L2-SAFE-004](moderation-trust-safety/L2.md#l2-safe-004-create-a-case-with-durable-evidence),
[L2-SAFE-008](moderation-trust-safety/L2.md#l2-safe-008-apply-one-auditable-moderation-action), and
[L2-SAFE-013](moderation-trust-safety/L2.md#l2-safe-013-enforce-the-moderation-action-effect-matrix).

The slice must traverse Angular route and UI, typed client, HTTP endpoint, Application use case,
Domain policy, relational persistence, post-commit event handling, and refreshed UI. It requires
domain, integration, component, live journey, accessibility, and published-artifact evidence before
any involved L2 is marked `implemented`.

## Open product decisions

These choices are intentionally left to a product specialization because their answers materially
change behavior or architecture. Until decided, the safest restrictive behavior applies.

| Decision | Default assumption | Owner | Evidence needed | Decision deadline or trigger |
| --- | --- | --- | --- | --- |
| Minimum participant age and guardian flow | Self-service participation is unavailable where age eligibility is unknown or unmet. | Product Owner with Privacy Owner | Target-market age rules, risk analysis, guardian research, consent lifecycle, and counsel input where applicable. | Before recruiting or enabling participants whose eligibility may depend on age. |
| Public Community and content indexing | Public visibility is opt-in and indexing is off until canonical-domain and moderation readiness. | Growth Owner with Trust and Safety Owner | Public-field inventory, owner research, abuse review, moderation capacity, canonical-domain and deindex rehearsal. | Before any Public Projection enters a production sitemap. |
| Payment provider and merchant model | Paid Plans remain disabled without provider configuration and financial review. | Finance Owner | Provider evaluation, merchant/tax/refund responsibilities, supported markets/currencies, threat model, and reconciliation rehearsal. | Before any paid control or claim is enabled in production. |
| Transactional email, push, SMS, and media providers | Development fakes are labeled; production starts only with reviewed providers and credentials. | Operations Owner | Vendor/security/privacy review, accessibility checks, delivery tests, callback and exit plan, cost and quota model. | Before the corresponding production channel is enabled. |
| Retention periods and legal holds | Minimize retained personal data and prevent unreviewed hard deletion where a hold may apply. | Privacy Owner | Purpose inventory, data map, deletion capability, legal input where applicable, user research, and timed cleanup evidence. | Before accepting lifecycle requirements or retaining production personal data. |
| Regional hosting and recovery targets | No residency or recovery claim is made. | Operations Owner | Customer needs, data-flow map, provider regions, dependency objectives, restore tests, cost, and risk acceptance. | Before a production region is selected or an availability/recovery commitment is sold. |
| Realtime topology | REST remains the source of truth; scale-out is not claimed. | Architecture Owner | Measured connection/concurrency profile, ordering needs, failure tests, infrastructure options, and operating cost. | Before one-node limits fail the accepted load target or multi-instance production promotion. |
| Split marketing and application origins | One origin and one deployable artifact. | Web Platform Owner | Ownership and release-cadence evidence, traffic/CMS/CDN need, cookie/CORS/CSRF review, redirects, observability, and rollback rehearsal. | Before independent host deployment or when an accepted split trigger is measured. |
