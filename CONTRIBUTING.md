# Contributing to Luna

Thank you for your interest in contributing to **Luna**.

Luna is a commercial, proprietary software project owned by **Stellara AI LLC**.
We welcome thoughtful contributions that align with the project’s goals, while
maintaining clear ownership and protection of intellectual property.

Please read this document carefully before submitting any contributions.

---

## Project Philosophy

Luna is built with a long-term focus on:
- Educational effectiveness
- Safety and trust (especially for minors)
- Production-grade reliability
- Clean architecture and clear domain boundaries

Contributions should reinforce these principles.

---

## Ownership and Licensing

By submitting any contribution (including code, documentation, tests, comments, or suggestions),
you agree to the following:

- Your contribution is **voluntarily provided**
- You grant **Stellara AI LLC** a **perpetual, irrevocable, worldwide, royalty-free license**
  to use, modify, distribute, sublicense, and commercialize your contribution
  as part of the Luna project or related products
- You retain ownership of your original work, but grant Stellara AI LLC full rights
  to incorporate it into Luna without restriction

This ensures Luna can remain commercially viable while benefiting from community input.

If you do **not** agree to these terms, please do not submit contributions.

---

## What Can Be Contributed

We welcome contributions in areas such as:

- Bug fixes
- Performance improvements
- Documentation
- Tests
- Developer tooling
- Non-core utilities
- Refactoring that improves clarity or maintainability

All contributions must align with Luna’s architectural principles and product goals.

---

## What Should Not Be Contributed

Please do **not** submit:

- Changes that expose proprietary teaching logic or internal decision systems
- Features that compromise student safety, privacy, or auditability
- Large architectural changes without prior discussion
- Code copied from third-party sources with incompatible licenses
- Experimental or speculative features not aligned with the roadmap

---

## Architecture Rules (Important)

Luna follows a **modular monolith** architecture with a clear path to microservices.

When contributing:

- Respect module boundaries (`Identity`, `Classroom`, `Students`, etc.)
- Do not introduce tight coupling between modules
- Do not create shared database tables across modules
- Use contracts from `Luna.Contracts` for cross-module communication
- Keep business logic out of the API gateway

Pull requests that violate these principles may be declined.

---

## Development Workflow

1. Fork the repository
2. Create a feature branch from `main`
3. Make focused, well-scoped changes
4. Add or update tests where appropriate
5. Ensure builds and tests pass
6. Submit a pull request with a clear description

Small, incremental PRs are preferred.

---

## Code Quality Expectations

- Follow existing coding conventions
- Write clear, readable code
- Favor explicitness over cleverness
- Include comments where intent is non-obvious
- Avoid introducing unnecessary dependencies

---

## Security & Responsible Disclosure

Because Luna is designed for use by students, including minors:

- **Do not** publicly disclose security vulnerabilities
- Report security issues privately (see `SECURITY.md`)
- Avoid logging or exposing sensitive data in code or tests

Security-related issues are taken seriously.

---

## Communication

Before starting large or complex work, we strongly recommend:
- Opening an issue
- Starting a discussion
- Getting alignment on direction

This helps avoid wasted effort and ensures contributions fit the roadmap.

---

## Final Notes

Submitting a contribution does **not** create any partnership, employment,
or ownership relationship with Stellara AI LLC.

We appreciate your interest and effort in making Luna better.