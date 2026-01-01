# ADR-0001: Modular Monolith Architecture

## Status
Accepted

## Date
2026-01-01

## Context

Luna is an AI-powered education platform intended to be long-lived, commercially operated,
and capable of scaling in both features and team size.

Early architectural decisions must balance:
- Speed of initial development
- Operational simplicity
- Clear domain ownership
- A credible path to future scale

A decision was required on whether to adopt:
- A full microservices architecture from the beginning, or
- A single monolithic application

## Decision

Luna will be implemented as a **modular monolith**.

The system will be deployed as a single executable initially, but internally organized
into **explicit domain modules** with clear boundaries, owned data, and versioned contracts.
These modules will be designed so they can later be extracted into independent microservices
without major refactoring.

## Rationale

A modular monolith provides the best balance for Luna’s current stage:

- Reduces operational and cognitive complexity during early development
- Avoids premature distributed systems concerns (networking, observability, failure modes)
- Encourages clear domain boundaries without forcing network boundaries
- Enables rapid iteration on core teaching intelligence
- Preserves a clean migration path to microservices when scale or team size demands it

Given Luna’s emphasis on correctness, safety, and pedagogical quality, early simplicity
is more valuable than early distribution.

## Alternatives Considered

### Full microservices from day one
- Rejected due to high operational overhead
- Increased deployment complexity
- Slower iteration during early product discovery

### Single monolithic application with shared data
- Rejected due to poor domain isolation
- High risk of tight coupling and long-term technical debt

## Consequences

### Positive
- Faster early development
- Simpler deployment and debugging
- Strong domain boundaries enforced through code structure
- Easier onboarding for contributors
- Clear future migration path

### Negative
- Requires discipline to maintain boundaries
- Some refactoring will still be needed when extracting services
- Does not independently scale modules until extraction

## Implementation Notes

- Each domain module owns its logic and persistence
- Cross-module communication uses shared contracts
- Each module maintains its own DbContext and migrations
- The API gateway is a thin host, not a business logic container

## Follow-up / Review

Revisit this decision if:
- Independent deployment of modules becomes necessary
- Multiple teams require separate release cycles
- Operational load demands independent scaling