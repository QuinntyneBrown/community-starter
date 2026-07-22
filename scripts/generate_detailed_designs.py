#!/usr/bin/env python3
"""Generate detailed feature designs from the repository's L1/L2 specifications.

The generator keeps requirement identifiers, parent links, and requirement text
anchored to docs/specs. Each L1 outcome becomes one vertical feature, and each
L2 behavior traced to that outcome receives its own sequence diagram.
"""

from __future__ import annotations

import re
from dataclasses import dataclass
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[1]
SPECS_ROOT = REPO_ROOT / "docs" / "specs"
DESIGNS_ROOT = REPO_ROOT / "docs" / "detailed-designs"


FEATURE_SLUGS = {
    "L1-ADMN-001": "administer-community-settings",
    "L1-ADMN-002": "control-platform-support-access",
    "L1-ADMN-003": "control-platform-configuration",
    "L1-ADMN-004": "deliver-privacy-safe-insights",
    "L1-COMM-001": "govern-community-identity",
    "L1-COMM-002": "control-membership-lifecycle",
    "L1-COMM-003": "delegate-community-authority",
    "L1-COMM-004": "preserve-community-integrity",
    "L1-COMM-005": "govern-space-participation",
    "L1-EVNT-001": "manage-event-lifecycle",
    "L1-EVNT-002": "enforce-event-access",
    "L1-EVNT-003": "manage-attendance",
    "L1-EVNT-004": "coordinate-safe-participation",
    "L1-CONT-001": "govern-post-lifecycle",
    "L1-CONT-002": "handle-attachments-safely",
    "L1-CONT-003": "support-comment-conversations",
    "L1-CONT-004": "preserve-content-integrity",
    "L1-QUAL-001": "build-layered-risk-evidence",
    "L1-QUAL-002": "keep-tests-deterministic",
    "L1-QUAL-003": "reproduce-local-change-hygiene",
    "L1-QUAL-004": "enforce-secure-artifact-promotion",
    "L1-QUAL-005": "operate-telemetry-and-recovery",
    "L1-QUAL-006": "keep-delivery-claims-honest",
    "L1-UXDS-001": "establish-visual-language",
    "L1-UXDS-002": "provide-angular-component-contracts",
    "L1-UXDS-003": "make-interaction-states-resilient",
    "L1-UXDS-004": "meet-accessibility-baseline",
    "L1-UXDS-006": "make-language-and-media-inclusive",
    "L1-DISC-001": "deliver-useful-feeds",
    "L1-DISC-002": "search-authorized-content",
    "L1-DISC-003": "record-engagement",
    "L1-DISC-004": "keep-discovery-trustworthy",
    "L1-AUTH-001": "manage-safe-account-lifecycle",
    "L1-AUTH-002": "protect-authenticated-sessions",
    "L1-AUTH-003": "enforce-server-authorization",
    "L1-AUTH-004": "observe-access-protection",
    "L1-MKTG-001": "deliver-truthful-public-surface",
    "L1-MKTG-002": "publish-unambiguous-routes",
    "L1-MKTG-003": "make-public-discovery-cache-safe",
    "L1-MKTG-004": "verify-public-deployment",
    "L1-MESS-001": "authorize-conversation-access",
    "L1-MESS-002": "deliver-durable-messages",
    "L1-MESS-003": "reconcile-realtime-experience",
    "L1-SAFE-001": "report-policy-harm",
    "L1-SAFE-002": "manage-moderation-cases",
    "L1-SAFE-003": "enforce-proportionate-actions",
    "L1-SAFE-004": "review-safety-decisions",
    "L1-NOTF-001": "deliver-private-notifications",
    "L1-NOTF-002": "control-recipient-attention",
    "L1-NOTF-003": "deliver-external-notifications",
    "L1-NOTF-004": "operate-notification-channels",
    "L1-ONBD-001": "complete-resumable-onboarding",
    "L1-ONBD-002": "offer-relevant-discovery",
    "L1-ONBD-003": "reach-first-community-value",
    "L1-ONBD-004": "keep-onboarding-safe",
    "L1-OPER-001": "observe-system-product-health",
    "L1-OPER-002": "measure-reliability-capacity",
    "L1-OPER-003": "respond-to-incidents",
    "L1-OPER-004": "recover-data-services",
    "L1-OPER-005": "control-production-change",
    "L1-ARCH-001": "preserve-full-stack-boundaries",
    "L1-ARCH-002": "keep-behavior-server-authoritative",
    "L1-ARCH-003": "protect-data-persistence-integrity",
    "L1-ARCH-004": "enforce-implementation-discipline",
    "L1-ARCH-005": "evolve-runtime-topology",
    "L1-ARCH-006": "trace-architecture-to-product-truth",
    "L1-PRIV-001": "minimize-personal-data-use",
    "L1-PRIV-002": "control-personal-data",
    "L1-PRIV-003": "govern-every-data-copy",
    "L1-PRIV-004": "enforce-provider-privacy",
    "L1-PROF-001": "present-trustworthy-profile",
    "L1-PROF-003": "enforce-relationship-safety",
    "L1-PROF-004": "respect-community-context",
    "L1-SECU-001": "model-threats-and-identity",
    "L1-SECU-002": "protect-secrets-diagnostics-data",
    "L1-SECU-003": "constrain-untrusted-boundaries",
    "L1-SECU-004": "harden-runtime-supply-chain",
    "L1-SECU-005": "operate-security-readiness",
    "L1-STRT-001": "reproduce-clean-checkout",
    "L1-STRT-002": "isolate-development-data",
    "L1-STRT-003": "specialize-starter-safely",
    "L1-STRT-004": "maintain-repository-knowledge",
}


KIND_DEFAULTS = {
    "product": {
        "system": "Community Platform",
        "system_desc": "Community participation, administration, and safety system",
        "frontend": "Member Web Application",
        "frontend_tech": "Angular",
        "backend": "Community API",
        "backend_tech": "ASP.NET Core",
        "domain": "Community Application and Domain",
        "worker": "Community Job Worker",
        "worker_tech": ".NET background worker",
        "infrastructure": "Community Infrastructure",
        "store": "Community Database",
        "store_tech": "Relational database",
        "external": "Delivery and Storage Providers",
        "external_desc": "Purpose-bound email, media, and infrastructure services",
        "surface_kind": "page component",
        "client_kind": "typed Angular client",
        "endpoint_kind": "HTTP endpoint",
    },
    "engineering": {
        "system": "Community Starter Delivery System",
        "system_desc": "Build, verification, promotion, and engineering evidence system",
        "frontend": "Engineering Workbench",
        "frontend_tech": "CLI and repository tools",
        "backend": "Delivery Control Plane",
        "backend_tech": "CI/CD workflows",
        "domain": "Delivery Policy and Evidence",
        "worker": "Verification Worker",
        "worker_tech": "Pipeline jobs",
        "infrastructure": "Build and Artifact Infrastructure",
        "store": "Evidence Registry",
        "store_tech": "Versioned artifact metadata",
        "external": "Package and Deployment Providers",
        "external_desc": "Toolchain, package, artifact, and environment services",
        "surface_kind": "engineering command surface",
        "client_kind": "typed workflow adapter",
        "endpoint_kind": "pipeline entry point",
    },
    "design": {
        "system": "Community Experience System",
        "system_desc": "Design tokens, Angular components, content, and accessibility contracts",
        "frontend": "Component Workbench",
        "frontend_tech": "Angular and Storybook",
        "backend": "Design Validation Pipeline",
        "backend_tech": "CI workflows",
        "domain": "Design Tokens and Component Policy",
        "worker": "Asset Verification Worker",
        "worker_tech": "Pipeline jobs",
        "infrastructure": "Design Artifact Infrastructure",
        "store": "Design Artifact Registry",
        "store_tech": "Versioned files",
        "external": "Browser and Assistive Technology Matrix",
        "external_desc": "Supported rendering and interaction environments",
        "surface_kind": "component workbench surface",
        "client_kind": "typed component adapter",
        "endpoint_kind": "validation entry point",
    },
    "marketing": {
        "system": "Community Public Web",
        "system_desc": "Static public content, Community previews, and application navigation",
        "frontend": "Public Website",
        "frontend_tech": "HTML, CSS, and TypeScript",
        "backend": "Public Web Publishing Pipeline",
        "backend_tech": "CI workflows",
        "domain": "Public Web Policy and Assembly",
        "worker": "Release Verification Worker",
        "worker_tech": "Pipeline jobs",
        "infrastructure": "Edge and Build Infrastructure",
        "store": "Public Artifact Manifest",
        "store_tech": "Versioned files",
        "external": "CDN and Search Crawlers",
        "external_desc": "Public caching, routing, and indexing systems",
        "surface_kind": "public page",
        "client_kind": "deployment configuration adapter",
        "endpoint_kind": "publishing entry point",
    },
    "operations": {
        "system": "Community Platform Operations",
        "system_desc": "Telemetry, incident, recovery, and production-change control system",
        "frontend": "Operations Console",
        "frontend_tech": "Web dashboard and runbook tools",
        "backend": "Operations Control API",
        "backend_tech": "Automation services",
        "domain": "Operations Policy and Automation",
        "worker": "Recovery and Reconciliation Worker",
        "worker_tech": "Automation jobs",
        "infrastructure": "Telemetry and Recovery Infrastructure",
        "store": "Operations Evidence Store",
        "store_tech": "Relational and object storage",
        "external": "Telemetry and Paging Providers",
        "external_desc": "Metrics, traces, logs, alerts, and responder routing",
        "surface_kind": "operations console surface",
        "client_kind": "typed operations adapter",
        "endpoint_kind": "operations command endpoint",
    },
    "security": {
        "system": "Community Platform Security",
        "system_desc": "Security policy, enforcement, evidence, and readiness system",
        "frontend": "Security Operations Console",
        "frontend_tech": "Web application and review tools",
        "backend": "Security Control API",
        "backend_tech": "Policy and automation services",
        "domain": "Security Policy and Enforcement",
        "worker": "Security Verification Worker",
        "worker_tech": "Scanning and evidence jobs",
        "infrastructure": "Key, Audit, and Scan Infrastructure",
        "store": "Security Evidence Store",
        "store_tech": "Protected relational and object storage",
        "external": "Identity, Secret, and Scan Providers",
        "external_desc": "Protected identity, key, and vulnerability services",
        "surface_kind": "security review surface",
        "client_kind": "typed security adapter",
        "endpoint_kind": "security control endpoint",
    },
    "starter": {
        "system": "Community Starter",
        "system_desc": "Reproducible development, specialization, and repository guidance system",
        "frontend": "Developer Workspace",
        "frontend_tech": "CLI and editor tasks",
        "backend": "Starter Tooling Runtime",
        "backend_tech": ".NET and Node.js tools",
        "domain": "Starter Configuration and Policy",
        "worker": "Validation and Setup Worker",
        "worker_tech": "Local automation",
        "infrastructure": "Local and Package Infrastructure",
        "store": "Starter Metadata Store",
        "store_tech": "Versioned files",
        "external": "Package Registries and Local Providers",
        "external_desc": "Toolchain, package, and development dependency services",
        "surface_kind": "developer command surface",
        "client_kind": "typed tooling adapter",
        "endpoint_kind": "tooling command entry point",
    },
}


SUBSYSTEM_KIND = {
    "administration-insights": "product",
    "communities-membership": "product",
    "community-events": "product",
    "content-media": "product",
    "delivery-quality": "engineering",
    "experience-design-system": "design",
    "feeds-search-engagement": "product",
    "identity-access": "product",
    "marketing-public-web": "marketing",
    "messaging-realtime": "product",
    "moderation-trust-safety": "product",
    "notifications-delivery": "product",
    "onboarding-discovery": "product",
    "operations-reliability": "operations",
    "platform-architecture": "engineering",
    "privacy-data-lifecycle": "product",
    "profiles-relationships": "product",
    "security": "security",
    "starter-experience": "starter",
}


SUBSYSTEM_ACTORS = {
    "administration-insights": ("Community Administrator", "Configures Communities and reviews bounded operational evidence"),
    "communities-membership": ("Community Member", "Creates, joins, and participates in Communities"),
    "community-events": ("Event Organizer", "Creates Events and coordinates eligible attendance"),
    "content-media": ("Community Member", "Publishes and discusses Community content"),
    "delivery-quality": ("Delivery Engineer", "Builds, verifies, and promotes release artifacts"),
    "experience-design-system": ("Product Engineer", "Builds product surfaces from shared interaction contracts"),
    "feeds-search-engagement": ("Community Member", "Discovers authorized content and records engagement"),
    "identity-access": ("Account Holder", "Establishes identity and requests protected actions"),
    "marketing-public-web": ("Visitor", "Reads public content and chooses an available application action"),
    "messaging-realtime": ("Community Member", "Participates in consent-based direct Conversations"),
    "moderation-trust-safety": ("Moderator", "Reviews Reports and applies bounded safety decisions"),
    "notifications-delivery": ("Account Holder", "Receives and controls eligible Notifications"),
    "onboarding-discovery": ("New Account Holder", "Completes onboarding and reaches a first Community"),
    "operations-reliability": ("Platform Operator", "Observes, restores, and changes the production service"),
    "platform-architecture": ("Product Engineer", "Implements and verifies full-stack feature slices"),
    "privacy-data-lifecycle": ("Account Holder", "Controls personal data and Account lifecycle choices"),
    "profiles-relationships": ("Community Member", "Presents identity and manages relationship boundaries"),
    "security": ("Security Engineer", "Reviews security boundaries and production evidence"),
    "starter-experience": ("Starter Adopter", "Configures and specializes a clean repository checkout"),
}


SUBSYSTEM_EXTERNALS = {
    "content-media": ("Media Storage and Scan Providers", "Stores quarantined media and reports content safety results"),
    "feeds-search-engagement": ("Search Infrastructure", "Indexes eligible records after authoritative commits"),
    "identity-access": ("Identity and Email Providers", "Performs configured identity proof and delivers private actions"),
    "messaging-realtime": ("Realtime Delivery Infrastructure", "Carries revocable hints after committed Message state"),
    "moderation-trust-safety": ("Notification and Evidence Providers", "Delivers notices and stores restricted evidence"),
    "notifications-delivery": ("Email Delivery Provider", "Attempts privacy-safe external email Delivery"),
    "onboarding-discovery": ("Notification Delivery", "Delivers eligible invitation and onboarding actions"),
    "privacy-data-lifecycle": ("Data Processors", "Process purpose-bound data under lifecycle controls"),
    "profiles-relationships": ("Media Storage Provider", "Stores authorized Profile image variants"),
}


@dataclass(frozen=True)
class Requirement:
    identifier: str
    title: str
    parent: str
    text: str


@dataclass(frozen=True)
class Feature:
    identifier: str
    title: str
    description: str
    requirements: tuple[Requirement, ...]


@dataclass(frozen=True)
class Subsystem:
    folder: str
    title: str
    context: str
    features: tuple[Feature, ...]


def compact(text: str) -> str:
    return re.sub(r"\s+", " ", text).strip()


def obligation_prose(text: str) -> str:
    result = compact(text)
    result = re.sub(r"\bmust not\b", "shall not", result, flags=re.IGNORECASE)
    result = re.sub(r"\bmust\b", "shall", result, flags=re.IGNORECASE)
    return result


def parse_subsystem(folder: Path) -> Subsystem:
    l1_text = (folder / "L1.md").read_text(encoding="utf-8")
    l2_text = (folder / "L2.md").read_text(encoding="utf-8")

    subsystem_match = re.search(r"^#\s+[^—]+—\s+(.+)$", l1_text, re.MULTILINE)
    context_match = re.search(
        r"^## Product context\s*\n\n(.+?)(?=\n\n## L1-)",
        l1_text,
        re.MULTILINE | re.DOTALL,
    )
    if not subsystem_match or not context_match:
        raise ValueError(f"Cannot parse subsystem header or context in {folder}")

    requirement_sections = re.finditer(
        r"^## (L2-[A-Z]+-[0-9]+): (.+?)\n(?P<body>.*?)(?=^## L2-|\Z)",
        l2_text,
        re.MULTILINE | re.DOTALL,
    )
    requirements: dict[str, Requirement] = {}
    for match in requirement_sections:
        body = match.group("body")
        parent_match = re.search(r"\*\*Traces to:\*\*\s+(L1-[A-Z]+-[0-9]+)", body)
        text_match = re.search(
            r"\*\*Related:\*\*.*?\n\n(.+?)(?=\n\n### Acceptance criteria)",
            body,
            re.DOTALL,
        )
        if not parent_match or not text_match:
            raise ValueError(f"Cannot parse trace or requirement text for {match.group(1)}")
        requirement = Requirement(
            identifier=match.group(1),
            title=match.group(2).strip(),
            parent=parent_match.group(1),
            text=compact(text_match.group(1)),
        )
        requirements[requirement.identifier] = requirement

    feature_sections = re.finditer(
        r"^## (L1-[A-Z]+-[0-9]+): (.+?)\n\n(?P<body>.*?)(?=^## L1-|\Z)",
        l1_text,
        re.MULTILINE | re.DOTALL,
    )
    features: list[Feature] = []
    assigned: set[str] = set()
    for match in feature_sections:
        identifier = match.group(1)
        body = match.group("body")
        description = body.split("\n\n| L2 |", 1)[0].strip()
        traced = tuple(req for req in requirements.values() if req.parent == identifier)
        if not traced:
            raise ValueError(f"{identifier} has no traced L2 requirements")
        features.append(
            Feature(
                identifier=identifier,
                title=match.group(2).strip(),
                description=description,
                requirements=traced,
            )
        )
        assigned.update(req.identifier for req in traced)

    unassigned = sorted(set(requirements) - assigned)
    if unassigned:
        raise ValueError(f"Unassigned L2 requirements in {folder.name}: {unassigned}")

    return Subsystem(
        folder=folder.name,
        title=subsystem_match.group(1).strip(),
        context=compact(context_match.group(1)),
        features=tuple(features),
    )


def profile_for(subsystem: str) -> dict[str, str]:
    profile = dict(KIND_DEFAULTS[SUBSYSTEM_KIND[subsystem]])
    profile["actor"], profile["actor_desc"] = SUBSYSTEM_ACTORS[subsystem]
    if subsystem in SUBSYSTEM_EXTERNALS:
        profile["external"], profile["external_desc"] = SUBSYSTEM_EXTERNALS[subsystem]
    return profile


def pascal(slug: str) -> str:
    return "".join(part.capitalize() for part in slug.split("-"))


def identifier_name(text: str) -> str:
    normalized = text.replace("C#", "CSharp")
    words = re.findall(r"[A-Za-z0-9]+", normalized)
    return "".join(word if word.isupper() else word[:1].upper() + word[1:] for word in words)


def lower_first(text: str) -> str:
    if not text or (len(text) > 1 and text[:2].isupper()):
        return text
    return text[:1].lower() + text[1:]


def join_phrases(items: list[str]) -> str:
    if len(items) == 1:
        return items[0]
    if len(items) == 2:
        return f"{items[0]} and {items[1]}"
    return f"{', '.join(items[:-1])}, and {items[-1]}"


def md_cell(text: str) -> str:
    return compact(text).replace("|", "\\|")


def puml(text: str) -> str:
    return compact(text).replace('"', "'")


def readme_for(subsystem: Subsystem, feature: Feature, slug: str, profile: dict[str, str]) -> str:
    base = pascal(slug)
    ids = join_phrases([f"`{req.identifier}`" for req in feature.requirements])
    behavior_names = join_phrases([lower_first(req.title) for req in feature.requirements])

    requirement_rows = "\n".join(
        f"| `{req.identifier}` | `{req.parent}` | {md_cell(req.text)} |"
        for req in feature.requirements
    )
    behavior_contracts = "\n".join(
        f"- **`{base}Policy.{identifier_name(req.title)}(record, request)`** — evaluates "
        f"`{req.identifier}` ({lower_first(req.title)}) and returns a typed decision before any "
        "state change."
        for req in feature.requirements
    )

    behavior_sections = []
    for req in feature.requirements:
        image_name = f"sequence-{req.identifier.lower()}.png"
        behavior_sections.append(
            f"""### Behaviour — {lower_first(req.title)}

The interaction loads current scoped state before `{base}Policy` enforces
`{req.identifier}`. Rejected decisions return without changing authoritative state; accepted
state changes commit before optional derived work starts.

![Sequence diagram for {lower_first(req.title)}](diagrams/{image_name})"""
        )

    return f"""# {feature.title}

## Overview

Community Starter is a community platform divided into product and platform subsystems. The
{subsystem.title} subsystem owns this feature.

*{lower_first(feature.title)}* — subsystem capability that covers {behavior_names}

{obligation_prose(subsystem.context)} {obligation_prose(feature.description)}

The feature groups {len(feature.requirements)} traced behaviors behind one policy and evidence
boundary: {ids}. Authoritative state commits before projections, delivery, or external work reports
success.

## Description

The repository contains specifications but no application implementation. This greenfield slice
defines the following building blocks across `{profile['frontend']}`, `{profile['backend']}`, the
application and domain layer, and infrastructure.

- **`{base}Surface`** — {profile['surface_kind']} in `{profile['frontend']}`. It presents current
  state, submits user intent, and reconciles the typed result.
- **`{base}Client`** — {profile['client_kind']}. It creates `{base}Request` values and maps stable
  transport failures into feature results.
- **`{base}Endpoint`** — {profile['endpoint_kind']} in `{profile['backend']}`. It authenticates the
  caller, applies boundary policy, and dispatches the request.
- **`{base}Request`** — immutable request carrying `SubjectId`, `Action`, `ExpectedVersion`, and the
  scoped input needed by one traced behavior.
- **`{base}Handler`** — application service that loads authorized state through
  `I{base}Repository`, invokes `{base}Policy`, and commits an accepted transition.
- **`{base}Policy`** — domain policy that evaluates current state and returns a typed
  `{base}Decision` without performing external work.
- **`{base}Record`** — authoritative record containing the feature state, scope, and concurrency
  version.
- **`I{base}Repository`** — persistence port that loads scoped state and commits one conditional
  unit of work.
- **`{base}Projector`** — idempotent post-commit component in `{profile['worker']}`. It updates
  eligible projections and invokes configured external providers.

`{base}Policy` exposes one named operation for each traced behavior:

{behavior_contracts}

## Requirements

The feature realizes the following level-2 (L2) requirements. Each row preserves the specification
identifier, its level-1 (L1) parent, and the requirement statement verbatim.

| L2 ID | Refines (L1) | Requirement |
|-------|--------------|-------------|
{requirement_rows}

## Diagrams

### System context

The `{profile['actor']}` uses `{profile['system']}` for the feature. The system invokes
`{profile['external']}` only for configured external work after authoritative decisions.

![C4 system context for {lower_first(feature.title)}](diagrams/c4-context.png)

### Containers

`{profile['frontend']}` collects intent, `{profile['backend']}` applies the synchronous boundary,
and `{profile['store']}` holds authoritative state. `{profile['worker']}` handles eligible
post-commit work against `{profile['external']}`.

![C4 container view for {lower_first(feature.title)}](diagrams/c4-container.png)

### Components

Inside `{profile['backend']}`, `{base}Endpoint` dispatches `{base}Handler`. The handler evaluates
`{base}Policy`, persists through `I{base}Repository`, and hands committed outcomes to
`{base}Projector`.

![C4 component view for {lower_first(feature.title)}](diagrams/c4-component.png)

### Class structure

`{base}Handler` depends on the immutable request, domain policy, and repository port.
`{base}Record` owns versioned state, while `{base}Projector` consumes committed results.

![Class diagram for {lower_first(feature.title)}](diagrams/class-structure.png)

{(chr(10) * 2).join(behavior_sections)}
"""


def c4_context(feature: Feature, profile: dict[str, str]) -> str:
    return f"""@startuml
!include <C4/C4_Context>
LAYOUT_WITH_LEGEND()
title System Context — {puml(feature.title)}

Person(actor, "{puml(profile['actor'])}", "{puml(profile['actor_desc'])}")
System(system, "{puml(profile['system'])}", "{puml(profile['system_desc'])}")
System_Ext(external, "{puml(profile['external'])}", "{puml(profile['external_desc'])}")

Rel(actor, system, "Uses for {puml(lower_first(feature.title))}", "HTTPS or local command")
Rel(system, external, "Invokes for eligible external work", "Purpose-bound contract")
@enduml
"""


def c4_container(feature: Feature, profile: dict[str, str]) -> str:
    return f"""@startuml
!include <C4/C4_Container>
LAYOUT_WITH_LEGEND()
title Containers — {puml(feature.title)}

Person(actor, "{puml(profile['actor'])}", "{puml(profile['actor_desc'])}")
System_Ext(external, "{puml(profile['external'])}", "{puml(profile['external_desc'])}")
System_Boundary(system, "{puml(profile['system'])}") {{
  Container(frontend, "{puml(profile['frontend'])}", "{puml(profile['frontend_tech'])}", "Collects intent and presents current state")
  Container(backend, "{puml(profile['backend'])}", "{puml(profile['backend_tech'])}", "Applies synchronous boundary and application policy")
  Container(worker, "{puml(profile['worker'])}", "{puml(profile['worker_tech'])}", "Processes idempotent post-commit work")
  ContainerDb(store, "{puml(profile['store'])}", "{puml(profile['store_tech'])}", "Stores authoritative state and durable outcomes")
}}

Rel(actor, frontend, "Uses", "HTTPS or local command")
Rel(frontend, backend, "Submits typed intent", "Typed contract")
Rel(backend, store, "Reads and conditionally writes", "Transactional adapter")
Rel(backend, worker, "Publishes committed outcomes", "Durable queue")
Rel(worker, store, "Reads outcomes and checkpoints", "Idempotent adapter")
Rel(worker, external, "Invokes eligible external work", "Purpose-bound contract")
@enduml
"""


def c4_component(feature: Feature, slug: str, profile: dict[str, str]) -> str:
    base = pascal(slug)
    return f"""@startuml
!include <C4/C4_Component>
LAYOUT_WITH_LEGEND()
title Components — {puml(feature.title)}

Container(frontend, "{puml(profile['frontend'])}", "{puml(profile['frontend_tech'])}", "Presents state and sends typed intent")
Container_Boundary(backend, "{puml(profile['backend'])}") {{
  Component(endpoint, "{base}Endpoint", "{puml(profile['endpoint_kind'])}", "Authenticates and dispatches feature requests")
  Component(handler, "{base}Handler", "Application service", "Coordinates authorized state and one unit of work")
  Component(policy, "{base}Policy", "Domain policy", "Evaluates traced behavior against current state")
  Component(repository, "I{base}Repository", "Persistence port", "Loads scoped state and commits conditionally")
  Component(projector, "{base}Projector", "Post-commit component", "Processes committed outcomes idempotently")
  ComponentDb(store, "{puml(profile['store'])}", "{puml(profile['store_tech'])}", "Stores state, versions, outcomes, and checkpoints")
}}

Rel(frontend, endpoint, "Submits {base}Request", "Typed contract")
Rel(endpoint, handler, "Dispatches request")
Rel(handler, policy, "Evaluates current state")
Rel(handler, repository, "Loads and commits")
Rel(repository, store, "Reads and writes", "Transactional adapter")
Rel(handler, projector, "Publishes committed outcome", "Durable handoff")
Rel(projector, store, "Checkpoints processing", "Idempotent adapter")
@enduml
"""


def class_structure(feature: Feature, slug: str) -> str:
    base = pascal(slug)
    policy_methods = "\n".join(
        f"  +{identifier_name(requirement.title)}(record: {base}Record, request: {base}Request): {base}Decision"
        for requirement in feature.requirements
    )
    return f"""@startuml
skinparam backgroundColor #FFFFFF
skinparam shadowing false
skinparam roundcorner 8
skinparam defaultFontName Arial
skinparam ArrowColor #344054
skinparam class {{
  BorderColor #344054
  FontColor #101828
  BackgroundColor #F9FAFB
}}
title Class structure — {puml(feature.title)}

class {base}Request {{
  +string SubjectId
  +string Action
  +long ExpectedVersion
  +Map<string, string> Input
}}

class {base}Decision {{
  +bool IsAccepted
  +string Code
  +Map<string, string> SafeDetails
}}

class {base}Record {{
  +string Id
  +string ScopeId
  +string State
  +long Version
  +Apply(decision: {base}Decision): void
}}

interface I{base}Repository {{
  +Load(subjectId: string): Task<{base}Record>
  +Commit(record: {base}Record, expectedVersion: long): Task<bool>
}}

class {base}Policy {{
  +Evaluate(record: {base}Record, request: {base}Request): {base}Decision
{policy_methods}
}}

class {base}Handler {{
  +Handle(request: {base}Request): Task<{base}Decision>
}}

class {base}Projector {{
  +Project(decision: {base}Decision): Task<void>
}}

{base}Handler ..> {base}Request : handles
{base}Handler ..> {base}Policy : evaluates with
{base}Handler --> I{base}Repository : loads / commits
I{base}Repository --> {base}Record : persists
{base}Policy ..> {base}Record : inspects
{base}Policy --> {base}Decision : produces
{base}Record ..> {base}Decision : applies accepted
{base}Projector ..> {base}Decision : consumes committed
@enduml
"""


def sequence(feature: Feature, requirement: Requirement, slug: str, profile: dict[str, str]) -> str:
    base = pascal(slug)
    return f"""@startuml
title UML sequence behaviour — {puml(requirement.title)}
skinparam backgroundColor #FFFFFF
skinparam shadowing false
skinparam roundcorner 12
skinparam defaultFontName Arial
skinparam ArrowColor #344054
skinparam rectangle {{
  BorderColor #344054
  FontColor #101828
}}
skinparam package {{
  BorderColor #667085
  FontColor #101828
  BackgroundColor #F9FAFB
}}
actor "{puml(profile['actor'])}" as actor
box "Frontend — {puml(profile['frontend'])}" #E0F2FE
  participant "{base}Surface" as surface
  participant "{base}Client" as client
end box
box "Backend — {puml(profile['backend'])}" #D1FADF
  participant "{base}Endpoint" as endpoint
end box
box "Backend — {puml(profile['domain'])}" #ECFDF3
  participant "{base}Handler" as handler
  participant "{base}Policy" as policy
end box
box "Backend — {puml(profile['infrastructure'])}" #FFF4E5
  database "{puml(profile['store'])}" as store
  participant "{base}Projector" as projector
end box

actor -> surface : Start {puml(lower_first(requirement.title))}
surface -> client : Build typed request
client -> endpoint : Submit request
endpoint -> endpoint : Authenticate and apply boundary policy
endpoint -> handler : Send {base}Request
handler -> store : Load current scoped state
store --> handler : Current state and version
handler -> policy : Enforce {requirement.identifier} — {puml(lower_first(requirement.title))}
policy --> handler : Typed decision
alt Decision is accepted
  opt Requirement changes authoritative state
    handler -> store : Commit state and outcome conditionally
    store --> handler : Commit result
  end
  opt Committed outcome has derived effects
    handler -> projector : Publish committed outcome
    projector --> handler : Accepted for idempotent processing
  end
  handler --> endpoint : Accepted feature result
else Decision is rejected or stale
  handler --> endpoint : Safe validation, denial, or conflict result
end
endpoint --> client : Typed response
client --> surface : Reconciled feature result
surface --> actor : Present current outcome
@enduml
"""


def write_feature(subsystem: Subsystem, feature: Feature) -> int:
    slug = FEATURE_SLUGS[feature.identifier]
    profile = profile_for(subsystem.folder)
    feature_root = DESIGNS_ROOT / subsystem.folder / slug
    diagrams = feature_root / "diagrams"
    diagrams.mkdir(parents=True, exist_ok=True)

    files = {
        feature_root / "README.md": readme_for(subsystem, feature, slug, profile),
        diagrams / "c4-context.puml": c4_context(feature, profile),
        diagrams / "c4-container.puml": c4_container(feature, profile),
        diagrams / "c4-component.puml": c4_component(feature, slug, profile),
        diagrams / "class-structure.puml": class_structure(feature, slug),
    }
    for requirement in feature.requirements:
        files[diagrams / f"sequence-{requirement.identifier.lower()}.puml"] = sequence(
            feature, requirement, slug, profile
        )

    for path, content in files.items():
        path.write_text(content.rstrip() + "\n", encoding="utf-8", newline="\n")
    return len(files) - 1


def main() -> int:
    if not SPECS_ROOT.is_dir():
        raise SystemExit("docs/specs is missing")

    subsystems = tuple(
        parse_subsystem(folder)
        for folder in sorted(SPECS_ROOT.iterdir())
        if folder.is_dir()
    )
    parsed_ids = {feature.identifier for subsystem in subsystems for feature in subsystem.features}
    missing_slugs = sorted(parsed_ids - FEATURE_SLUGS.keys())
    stale_slugs = sorted(FEATURE_SLUGS.keys() - parsed_ids)
    missing_profiles = sorted({subsystem.folder for subsystem in subsystems} - SUBSYSTEM_KIND.keys())
    if missing_slugs or stale_slugs or missing_profiles:
        raise SystemExit(
            f"Feature map mismatch: missing={missing_slugs}, stale={stale_slugs}, "
            f"profiles={missing_profiles}"
        )

    feature_count = 0
    requirement_count = 0
    diagram_count = 0
    for subsystem in subsystems:
        for feature in subsystem.features:
            feature_count += 1
            requirement_count += len(feature.requirements)
            diagram_count += write_feature(subsystem, feature)

    print(
        f"Generated {feature_count} feature designs for {len(subsystems)} subsystems, "
        f"covering {requirement_count} L2 requirements with {diagram_count} diagrams."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
