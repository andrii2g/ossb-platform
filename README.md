# OSBB Platform

OSBB Platform is a monorepo for digital tools and services that support condominium management in Ukraine.

OSBB stands for the Ukrainian housing association model commonly translated as a condominium or apartment building co-ownership association. This repository is intended to host infrastructure, shared libraries, and domain-oriented services for managing units, billing, residents, notifications, maintenance workflows, and future integrations.

The first planned tool in this repository is the Invoice Generator. Additional modules will be introduced incrementally as the platform evolves.

## Status

The repository is currently in the bootstrap phase.

## Planned Modules

- Billing
- Payments reconciliation
- Residents registry
- Notifications
- Maintenance requests
- Document storage
- Voting and governance

## Repository Structure

```text
osbb-platform/
|-- .github/
|   |-- README.md
|   `-- workflows/
|       `-- ci.yml
|-- docs/
|   |-- architecture.md
|   |-- modules.md
|   `-- roadmap.md
|-- infra/
|   |-- README.md
|   |-- docker/
|   |   `-- README.md
|   `-- env/
|       `-- README.md
|-- libs/
|   |-- README.md
|   |-- contracts/
|   |   `-- README.md
|   `-- core/
|       `-- README.md
|-- scripts/
|   |-- README.md
|   `-- bootstrap/
|       `-- init-repo.sh
|-- services/
|   |-- README.md
|   |-- billing/
|   |   `-- README.md
|   |-- maintenance/
|   |   `-- README.md
|   |-- notifications/
|   |   `-- README.md
|   |-- payments/
|   |   `-- README.md
|   `-- residents/
|       `-- README.md
|-- tools/
|   |-- README.md
|   `-- invoice-generator/
|       `-- README.md
|-- .editorconfig
|-- .gitignore
`-- LICENSE
```

## Repository Principles

- Keep business domains isolated and easy to evolve.
- Prefer shared contracts and reusable libraries over ad hoc duplication.
- Keep tooling and infrastructure close to the code they support.
- Start modular inside the monorepo, then split operationally only when justified.

## Near-Term Direction

Near-term work will focus on repository bootstrap, developer workflow basics, and the first operational tool: Invoice Generator. Domain services will be added as small, focused projects with shared contracts and infrastructure conventions.
