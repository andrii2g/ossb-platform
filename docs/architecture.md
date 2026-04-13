# Architecture

## Overview

OSBB Platform is organized as a monorepo intended for a growing set of services, tools, shared libraries, and infrastructure assets related to condominium management in Ukraine.

The repository is intentionally structured to support future `.NET` services and utilities without forcing implementation decisions too early. Early modules can start as small, modular projects inside the same repository and later be extracted or deployed independently if operational needs justify it.

## Monorepo Approach

The monorepo layout is intended to provide:

- Shared visibility across business domains
- Consistent engineering standards and tooling
- Centralized infrastructure and environment conventions
- Reusable contracts and common libraries
- Easier coordination across services and supporting tools

This structure is suitable for a platform that will likely contain a mix of APIs, background jobs, operational tools, integration adapters, and internal libraries.

## Proposed Domains

### Billing

Handles invoice generation, charge calculations, debt tracking, billing periods, and future accounting-related workflows.

### Payments

Focuses on payment ingestion, reconciliation, ledger alignment, and integration with banking or payment providers.

### Residents

Maintains resident and unit-related records, ownership associations, tenancy metadata, and administrative lookup data.

### Notifications

Provides outbound communication capabilities such as email, SMS, push messages, and event-driven notification workflows.

### Maintenance

Supports maintenance requests, issue tracking, task handling, contractor coordination, and service history.

### Shared Libraries

Contains reusable core utilities, domain abstractions, and contracts shared across services and tools.

### Infrastructure

Holds operational assets such as container definitions, environment templates, deployment helpers, and bootstrap scripts.

## Evolution Note

Services may initially be implemented as modular projects within the monorepo to keep development simple and coordinated. If scale, deployment independence, or team boundaries require it later, individual services can be split operationally without changing the overall domain model of the repository.
