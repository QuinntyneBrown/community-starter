# Community Starter

[![Quality](https://github.com/QuinntyneBrown/community-starter/actions/workflows/quality.yml/badge.svg)](https://github.com/QuinntyneBrown/community-starter/actions/workflows/quality.yml)

Community Starter is a production-oriented reference implementation for building and operating
multi-community products. It combines an ASP.NET Core API, an Angular member application, a static
Astro site, PostgreSQL, and a shared design system in one traceable repository.

The project is designed around server-owned community rules and vertical feature slices. Product
requirements in [`docs/specs`](docs/specs/README.md) trace into detailed designs in
[`docs/detailed-designs`](docs/detailed-designs/) and then into implementation and verification
evidence.

> [!IMPORTANT]
> **Project status:** active, early-stage development. The requirement catalog is a draft and its
> requirements remain `proposed`. An initial full-stack slice is executable, but independent
> security, privacy, accessibility, recovery, load, and operational reviews are not recorded as
> complete. This repository does not make a production-readiness claim.

## What is included

- Account registration, verification, server-side sessions, and sign-out.
- Community creation, settings, invitations, and membership acceptance.
- Community-scoped feeds, posts, editing, and reactions.
- Reporting, moderation cases, and bounded moderation actions.
- Static public marketing and an authenticated Angular member surface.
- PostgreSQL persistence, a durable worker role, health checks, SignalR, and OpenTelemetry wiring.
- Generated feature contracts that keep all 260 L2 requirement identifiers visible without
  presenting every designed capability as implemented.

## Quick start

### Create a new project

Generate an independently named project in a new directory while leaving this reusable repository
unchanged:

```powershell
./eng/new-project.ps1 `
  -DisplayName "Harbor Circle" `
  -CodeName "HarborCircle" `
  -OutputPath ../harbor-circle
```

The command derives package, database, deployment, selector, and cookie identifiers from the safe
PascalCase code name. It rewrites requirements and generated designs, re-renders affected diagrams,
runs the complete quality gate, audits the result for residual template identity, and initializes a
fresh `main` Git repository without a commit or inherited remote. The destination must not already
exist and must be outside this repository.

Use `-SkipQualityCheck` only when testing the transformation itself; it skips the .NET and web build
gate but retains identity, requirements, diagram, and detailed-design verification.

### Prerequisites

- [PowerShell 7](https://learn.microsoft.com/powershell/scripting/install/installing-powershell)
- [.NET SDK 10.0.101](https://dotnet.microsoft.com/download) or a compatible 10.0 patch
- [Node.js 24.18.0](https://nodejs.org/) and npm 11.6.2
- [Python 3.13](https://www.python.org/downloads/) for design and quality checks
- [Java 17 or later](https://adoptium.net/) for rendering specialized PlantUML diagrams
- [Docker](https://docs.docker.com/get-docker/) with Docker Compose

The repository pins the .NET and Node.js versions in [`global.json`](global.json) and
[`.nvmrc`](.nvmrc).

### Run the containerized application

```powershell
git clone https://github.com/QuinntyneBrown/community-starter.git
Set-Location community-starter
Copy-Item .env.example .env
# Replace every placeholder value in .env before continuing.
./eng/start.ps1 -Containerized
```

Open <http://localhost:8080>. The same-origin artifact serves the public site, member application,
API, realtime hub, and health endpoints.

Stop the environment without deleting local data:

```powershell
./eng/stop.ps1
```

To also delete the local PostgreSQL, object-storage, and antivirus volumes, run
`./eng/stop.ps1 -RemoveVolumes`. This is destructive and cannot be undone through the repository.

### Run in developer mode

Install the web dependencies once:

```powershell
npm ci
```

Run the API and local dependencies in one terminal:

```powershell
./eng/start.ps1
```

Run either web surface with live reload in another terminal:

```powershell
npm run dev:marketing # http://localhost:4321
npm run dev:app       # http://localhost:4200
```

## Verify a change

Run the same complete quality gate used as the local handoff command:

```powershell
./eng/check.ps1
```

The script verifies generated designs and feature contracts, restores and audits dependencies,
builds and tests the backend, checks web formatting, tests and builds both web applications, and
audits npm dependencies.

The underlying commands are available when working on one layer:

| Scope                        | Command                                                                          |
| ---------------------------- | -------------------------------------------------------------------------------- |
| Verify detailed designs      | `python scripts/verify_detailed_designs.py`                                      |
| Test project specialization  | `npm run test:specialization`                                                    |
| Regenerate feature contracts | `python scripts/generate_feature_contracts.py`                                   |
| Restore backend              | `dotnet restore backend/CommunityStarter.sln --locked-mode`                      |
| Build backend                | `dotnet build backend/CommunityStarter.sln --configuration Release --no-restore` |
| Test backend                 | `dotnet test backend/CommunityStarter.sln --configuration Release --no-build`    |
| Install web dependencies     | `npm ci`                                                                         |
| Check web formatting         | `npm run format:check`                                                           |
| Test member application      | `npm run test`                                                                   |
| Build both web applications  | `npm run build`                                                                  |
| Build the production image   | `docker build --tag community-starter:local .`                                   |

A standalone end-to-end, accessibility, published-artifact smoke, migration rehearsal, or load-test
command is not implemented yet. Passing `./eng/check.ps1` must not be interpreted as evidence for
those review scopes.

## Repository layout

| Path                                              | Responsibility                                                                   |
| ------------------------------------------------- | -------------------------------------------------------------------------------- |
| [`backend`](backend/)                             | ASP.NET Core API, application and domain policy, persistence, workers, and tests |
| [`frontend`](frontend/)                           | Angular member application                                                       |
| [`marketing`](marketing/)                         | Statically generated Astro public site                                           |
| [`design-system`](design-system/)                 | Shared tokens, base styles, and original assets                                  |
| [`docs/specs`](docs/specs/)                       | L1 outcomes, L2 behaviors, acceptance criteria, and coverage status              |
| [`docs/detailed-designs`](docs/detailed-designs/) | Requirement-traced feature designs and rendered diagrams                         |
| [`eng`](eng/)                                     | Canonical local start, stop, and verification entry points                       |
| [`infra`](infra/)                                 | Docker Compose and Helm deployment definitions                                   |
| [`.github/workflows`](.github/workflows/)         | Continuous integration quality gate                                              |

## Architecture

Community Starter starts as a cohesive modular monolith. Backend dependencies point inward through
`API -> Infrastructure -> Application -> Domain`; product policy remains isolated from protocols,
providers, and deployment details. State changes commit authoritative data, audit records, outbox
messages, and jobs before derived delivery reports success.

The production image owns these routes:

| Route                | Owner                             |
| -------------------- | --------------------------------- |
| `/` and public pages | Static Astro output               |
| `/app/**`            | Angular member application        |
| `/api/**`            | ASP.NET Core HTTP API             |
| `/hubs/**`           | Authenticated SignalR connections |
| `/health/live`       | Process liveness                  |
| `/health/ready`      | Dependency readiness              |

Read [`docs/architecture.md`](docs/architecture.md) for dependency direction, the authoritative
change sequence, and current platform decisions.

## Configuration

- [`.env.example`](.env.example) lists the local Compose credentials that must be replaced in
  `.env`. Never commit `.env` or real credentials.
- [`appsettings.json`](backend/src/CommunityStarter.Api/appsettings.json) defines safe configuration
  shape and non-secret defaults.
- [`appsettings.Development.json`](backend/src/CommunityStarter.Api/appsettings.Development.json)
  contains unmistakably development-only values and enables token exposure for local workflows.
- ASP.NET Core environment variables use double underscores, such as
  `ConnectionStrings__Community`, `Security__TokenPepper`, `Migration__ApplyOnStartup`, and
  `Runtime__Role`.
- [`infra/compose.yaml`](infra/compose.yaml) is the local dependency topology;
  [`infra/helm/community-starter`](infra/helm/community-starter/) is the initial Kubernetes package.

## Documentation

- [MVP requirements and coverage register](docs/specs/README.md)
- [Detailed feature designs](docs/detailed-designs/)
- [Architecture overview](docs/architecture.md)
- [Shared design system](design-system/README.md)
- [Continuous integration workflow](.github/workflows/quality.yml)

The specifications are the source of truth for scope and status. A design document describes an
intended solution; it is not implementation or production evidence by itself.

## Contributing

Issues and pull requests are welcome while the project is under active development. Before opening
a pull request:

1. Link the affected L1/L2 requirement or explain why the change is outside the current catalog.
2. Update affected requirements, designs, architecture, configuration, and operational notes in the
   same change.
3. Run `./eng/check.ps1` and report the exact commands and results.
4. Include screenshots for meaningful UI changes and identify architecture, data, security,
   privacy, accessibility, deployment, risk, and rollback effects where applicable.
5. Do not mark a requirement `implemented` without evidence at every verification layer it names.

Use the [issue tracker](https://github.com/QuinntyneBrown/community-starter/issues) for confirmed
defects, scoped feature proposals, and contributor coordination.

## Security

Do not report suspected vulnerabilities in a public issue. Use GitHub's
[private vulnerability reporting](https://github.com/QuinntyneBrown/community-starter/security/advisories/new)
to share a minimal reproduction and impact assessment without exposing secrets or personal data.

## License

This repository does not currently contain a `LICENSE` file. Until an approved license is added,
the source is available for inspection, but no open-source reuse or distribution permission is
granted. Adding a license is required before describing the project as released open-source
software.
