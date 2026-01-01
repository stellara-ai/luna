# ADR-0000: <Short, Descriptive Title>

## Status
Proposed | Accepted | Deprecated | Superseded

## Date
YYYY-MM-DD

## Context

Describe the problem or decision that needs to be made.

Include:
- What prompted this decision
- Business or product constraints
- Technical constraints
- Relevant background or prior decisions

Keep this concise but complete.

---

## Decision

State the decision clearly and unambiguously.

Example:
> We will implement Luna as a modular monolith using .NET 10, with explicit domain boundaries and versioned contracts, and defer microservice extraction until justified by scale or team needs.

---

## Rationale

Explain **why** this decision was made.

Include:
- Benefits of the chosen approach
- Tradeoffs considered
- Why alternatives were rejected

This is the most important section.

---

## Alternatives Considered

List the main alternatives and why they were not chosen.

Example:
- Option A: Full microservices from day one
- Option B: Single monolithic application with shared database
- Option C: Serverless-only architecture

---

## Consequences

Describe the expected outcomes of this decision.

### Positive
- What this enables
- What becomes simpler or safer

### Negative
- What becomes harder
- Risks introduced
- Future migration costs (if any)

Be honest.

---

## Implementation Notes

(Optional but recommended)

- Key files or modules affected
- Migration steps (if applicable)
- Rollout considerations

---

## Follow-up / Review

- When should this decision be revisited?
- What signals would trigger a change?

Example:
> Revisit when concurrent active sessions exceed X, or when multiple teams require independent deployment.

---

## References

(Optional)

- Links to relevant docs
- Related ADRs
- External resources