# Copilot Instructions — Luna Backend

You are coding inside the Luna backend repository. ALWAYS align with `LUNE_CONTEXT.md` (Architecture Context & Project Vision). Treat it as the source of truth.

## Non-Negotiables (North Star)
- Luna optimizes for **conversational presence** and **felt responsiveness**, not request/response latency.
- Design for **streaming cognition**, **micro-acknowledgements**, **overlapping turns**, and **latency masking**.
- A technically perfect response delivered too late is a failure.

## Architecture Rules
- This is a **modular monolith first**, microservices-ready later.
- Maintain **hard domain boundaries**:
  - Do not introduce cross-module coupling via concrete types.
  - Prefer interfaces and explicit contracts.
  - Cross-module communication occurs through shared contracts (`shared/Luna.Contracts`) or well-defined abstractions.
- Do not share persistence across modules (no cross-module DbContext usage).
- Keep the host (`apps/Luna.ApiGateway`) thin: composition + routing only.

## Code Quality / Style
- Target .NET: `net10.0`
- `Nullable` enabled: fix nullability properly (prefer `required`, constructors, or safe defaults).
- Prefer small, testable units. Keep side effects at boundaries.
- Use dependency injection consistently. Use `ISystemClock` (not `DateTime.UtcNow`) in domain/session logic.

## Real-time / WebSockets
- WebSocket messages must use:
  - Typed envelopes (`WsEnvelope<TPayload>`)
  - `MessageId`, `CorrelationId`, `MessageType`, `Timestamp`, and optional `SequenceNumber`
  - Versioned message types (prefix `v1.`), e.g. `v1.classroom.student_input`
- Always include correlation handling:
  - If missing, derive from `CorrelationId ?? MessageId ?? new Guid`
- When emitting events:
  - Ensure `Sequence` is monotonic and 1-based
  - Ensure timestamps are set via `ISystemClock.UtcNow`
- Design for streaming responses (partial chunks) even if the first implementation is text-only.

## Safety / Auditability
- Every teaching turn and session action must be logged as an append-only session event.
- Never drop events silently. If an input is invalid, respond with a structured error envelope (`v1.classroom.error`) and log an event.

## When implementing features
- Implement the **minimal end-to-end slice** first (REST create session → WS connect → send student input → receive teacher response).
- Prefer deterministic orchestration around AI (structured inputs/outputs) rather than ad-hoc prompts.
- Add tests or a smoke-test script when introducing new WS message behavior.

## Output format expectation
- Provide complete, compilable code changes.
- If multiple files are needed, clearly indicate file paths and exact code blocks per file.
- Do NOT rename modules or reorganize project structure unless explicitly requested.