# ADR-0003: Dev JWT Authentication Strategy

## Status
Accepted

## Date
2026-01-01

## Context

Luna requires authenticated access to:
- Teaching sessions
- Student data
- Parental controls
- Audit logs

However, early development must remain fast and flexible while preserving a
production-shaped authentication model.

A decision was required on how to authenticate users during early development.

## Decision

Luna will use a **Dev JWT authentication strategy** during early development.

A lightweight authentication endpoint issues signed JWTs containing:
- User identity
- Role (student, parent)
- Tenant identifier

These tokens are used for API and WebSocket authentication and authorization.

## Rationale

Dev JWT provides:
- Production-shaped authentication flows
- Role and tenant enforcement
- Minimal setup and operational overhead
- Compatibility with WebSockets
- Easy replacement with OIDC or managed identity later

This approach avoids hard-coding identities or bypassing auth entirely, which would
increase refactoring risk later.

## Alternatives Considered

### No authentication (development-only)
- Rejected due to lack of security boundaries
- Difficult to retrofit later

### Full OIDC from day one
- Rejected due to setup complexity
- Slower early development
- Unnecessary for initial scope

### API keys or session tokens
- Rejected due to poor role modeling and scalability

## Consequences

### Positive
- Clear identity and authorization model
- Minimal friction for development
- WebSocket-friendly
- Smooth migration path to managed identity providers

### Negative
- Not suitable for production without replacement
- Requires careful token handling discipline

## Implementation Notes

- Tokens are short-lived
- Claims include user ID, role, and tenant
- Token validation is enforced for APIs and WebSockets
- Replacement with OIDC is planned before public release

## Follow-up / Review

Revisit this decision when:
- External users are onboarded
- Compliance requirements increase
- Production deployment is imminent