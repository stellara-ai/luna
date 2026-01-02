# AI Rules for Luna

These rules apply to all AI-generated code, design, and recommendations.

## Governing Documents
- LUNE_CONTEXT.md is the source of truth
- §2.1 Conversational Presence & Zero-Latency Teaching is non-negotiable

## Hard Constraints
- Modular monolith boundaries must be preserved
- No cross-module persistence or domain coupling
- WebSocket schemas are stable APIs
- All real-time messages must include:
  - correlation id
  - timestamp (via ISystemClock)
  - monotonic sequence number
- Streaming-first design is required whenever possible

## Forbidden Patterns
- Chatbot-style request/response flows
- Blocking or batch-only AI responses
- Silent latency without masking
- Implicit contracts between modules

## Priority Order
1. Conversational presence
2. Learning effectiveness
3. Safety & auditability
4. Performance
5. Developer convenience