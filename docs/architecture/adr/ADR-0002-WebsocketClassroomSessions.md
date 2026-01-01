# ADR-0002: WebSocket-Based Classroom Sessions

## Status
Accepted

## Date
2026-01-01

## Context

Luna’s core interaction model is a **live, conversational teaching session** between
a student and an AI teacher.

The system must support:
- Real-time student input
- Streaming teacher responses
- Low-latency interaction
- Future audio input/output
- Session state continuity

A decision was required on the communication mechanism for classroom sessions.

## Decision

Luna will use **WebSockets** as the primary transport for real-time classroom sessions.

Each teaching session will establish a persistent WebSocket connection through which:
- Student inputs are sent
- Teacher responses are streamed incrementally
- Session lifecycle events are emitted

## Rationale

WebSockets provide:
- Full-duplex communication
- Low latency suitable for conversational interaction
- A single, long-lived connection per session
- A natural fit for streaming text and audio

While alternatives such as Server-Sent Events (SSE) were considered, WebSockets better
support future requirements including speech-to-text streaming, interruption handling,
and richer real-time controls.

## Alternatives Considered

### HTTP request/response
- Rejected due to high latency and poor conversational flow

### Server-Sent Events (SSE)
- Simpler initially
- Rejected due to lack of client-to-server streaming support

### WebRTC
- Powerful but overly complex for early-stage requirements
- Deferred until visual or peer-to-peer needs arise

## Consequences

### Positive
- Enables real-time, voice-first interaction
- Supports incremental response streaming
- Simplifies session lifecycle management
- Aligns with future audio and avatar capabilities

### Negative
- Requires connection management and reconnection logic
- More complex infrastructure than pure HTTP
- Scaling requires careful state management

## Implementation Notes

- WebSocket messages use typed envelopes with correlation IDs
- Sessions require an explicit join step
- Events are append-only and auditable
- Reconnection and resume are planned from the start

## Follow-up / Review

Revisit if:
- Peer-to-peer video becomes a core requirement
- Session load exceeds infrastructure limits
- WebRTC becomes necessary for avatar rendering