# Luna — Architecture Context & Project Vision

## 1. Project Overview

**Luna** is an AI-powered homeschool and tutoring platform whose ultimate goal is to bring
the *best teachers into the home* through a **live, adaptive, conversational AI teacher**.

Luna is **not a chatbot** and **not a static learning app**.

The system is designed to:
- Adapt to individual student needs
- Support neurodivergent learners (starting with ADD / ADHD)
- Adjust pacing, explanation style, analogies, repetition, and reinforcement
- Teach through **spoken conversation**, not text-first learning
- Eventually present as a **live virtual teacher** (voice first, visuals later)

---

## 2. Ultimate Goal (North Star)

The long-term vision for Luna is:

> **A live, AI-generated teacher that teaches like a great human tutor**  
> — with voice, cadence, pauses, gestures, visuals, and adaptive pedagogy —
> continuously optimized to the student’s learning style, attention, and comprehension.

Key characteristics of the end-state:
- Voice-first, real-time interaction
- Teacher reasoning engine (not scripted responses)
- Visual teaching tools (whiteboard, slides, diagrams)
- Optional AI avatar (last, not first)
- Continuous learning optimization via feedback loops
- Safe, auditable, parent-visible learning sessions

Luna prioritizes **learning effectiveness over novelty**.

## 2.1 Conversational Presence & Zero-Latency Teaching (Non-Negotiable)

A core, non-negotiable requirement of Luna is that **interaction with the AI teacher must feel alive, immediate, and human**.

Luna explicitly optimizes for **conversational presence**, not traditional request/response latency.

This means:

- The teacher begins responding **before the student finishes speaking**
- Responses may arrive as **partial thoughts**, pauses, corrections, and continuations
- Silence, hesitation, and pacing are treated as **instructional signals**, not errors
- The system prioritizes **felt responsiveness** over perfectly formed answers

The student should experience Luna as a **thinking, listening teacher**, not a system that waits, computes, and replies.

### Design Principles for Conversational Realism

- **Streaming cognition**  
  Teacher reasoning and explanation may stream incrementally, mirroring how humans think aloud.

- **Overlapping turns**  
  The teacher may acknowledge, interrupt gently, or redirect mid-input when pedagogically appropriate.

- **Micro-acknowledgements**  
  Short verbal cues (“mm-hmm”, “okay”, “wait—good question”) are first-class responses.

- **Latency masking**  
  When unavoidable delays exist, Luna must use verbal fillers, partial explanations, or transitional cues to preserve conversational flow.

- **Voice-first orchestration**  
  Audio response timing and cadence take precedence over text completeness.

Luna treats **perceived latency** as a learning obstacle and actively works to eliminate it — even if this requires:

- speculative responses  
- predictive turn-taking  
- streaming partial outputs  
- concurrent input/output processing  
- bleeding-edge real-time model integration  

A technically perfect response delivered too late is considered a **failure**.

---

## 3. Architectural Philosophy

### 3.1 Modular Monolith First, Microservices Ready

Luna is intentionally built as a **modular monolith** initially, with **hard domain boundaries**
so that individual modules can later be split into independent microservices without rewrite.

Rules:
- One deployable host today
- Clear domain ownership
- No cross-module database writes
- Contracts are versioned and explicit
- Each module can become its own service later

This avoids premature distributed complexity while preserving long-term scalability.

---

## 4. Core Technical Stack

### Backend
- **.NET 10 LTS**
- ASP.NET Core (Minimal APIs)
- WebSockets for real-time classroom sessions
- PostgreSQL (per-module DbContexts)
- Redis (session state, real-time coordination)
- OpenTelemetry (logging, tracing, metrics)
- Docker (from day one)
- AWS target (ECS Fargate initially)

### Frontend
- Vue 3 + TypeScript
- Vite
- WebSockets (real-time streaming)
- Voice-first UX (TTS now, STT next)
- ADHD-friendly UI patterns

---

## 5. Core Backend Domains (Modules)

Each module is isolated and owns its data and logic.

### Identity (`Luna.Identity`)
- Dev JWT authentication (initial)
- User identity (student, parent)
- Tenant isolation
- Future OIDC integration

### Classroom (`Luna.Classroom`) — *Core Domain*
- Real-time teaching sessions
- WebSocket orchestration
- Lesson state machine
- Teaching turn execution
- Streaming teacher responses
- Session event logging

### Students (`Luna.Students`)
- Student profiles
- Learning preferences
- ADHD accommodations
- Attention and pacing metadata

### Curriculum (`Luna.Curriculum`)
- Subjects and lesson definitions
- Scope & sequence
- Teaching strategies
- Pedagogical metadata

### Media (`Luna.Media`)
- Text-to-Speech providers
- Speech-to-Text providers
- Streaming audio orchestration
- Provider abstraction layer

---

## 6. Real-Time Classroom Model

### WebSocket-Based Session

Each classroom session uses a **single WebSocket connection**:

- Client → Server:
  - student input (text now, audio later)
  - control signals (repeat, slower, confused)
- Server → Client:
  - streamed teacher text deltas
  - TTS audio chunks
  - turn lifecycle events

### Message Design Principles
- Typed envelopes
- Correlation IDs
- Sequence numbers
- Timestamped
- Append-only session event log

This enables:
- Replayability
- Debugging
- Auditability
- Future analytics

---

## 7. Teaching System (Conceptual)

Luna separates **how a teacher thinks** from **how the teacher is presented**.

Core loop:
1. Receive student input
2. Interpret understanding + attention
3. Select teaching strategy
4. Generate explanation
5. Ask comprehension check
6. Update student model
7. Repeat

Teaching intelligence is **model-driven**, not UI-driven.

---

## 8. AI Usage Philosophy

AI models are used as **reasoning engines**, not free-form chat.

Rules:
- Structured inputs (student profile, lesson state)
- Structured outputs (next action, explanation, strategy)
- Deterministic orchestration around the model
- All AI output is inspectable and auditable

---

## 9. Safety & Trust

Luna is built with minors in mind.

Non-negotiables:
- Session transcripts available to parents
- Event logs for every teaching turn
- Content boundaries enforced by lesson context
- Kill-switch capability for sessions
- Clear separation of student vs parent roles

---

## 10. What Luna Is NOT

- Not a generic chatbot
- Not a video-first gimmick
- Not a content library
- Not a “prompt playground”
- Not optimized for shortcuts or automation of thinking

Luna optimizes for **understanding**.

---

## 11. Development Guidance for AI Assistants (Copilot / ChatGPT)

When generating code or suggestions for Luna:
- Follow modular monolith boundaries
- Do not introduce tight coupling between modules
- Prefer explicit contracts over implicit assumptions
- Treat WebSocket schemas as stable APIs
- Design everything to be observable and debuggable
- Assume future microservice extraction

---

## 12. Summary

Luna is a **long-horizon product**.

Short-term:
- Voice-first AI teacher
- Real-time adaptive tutoring
- One student, one subject MVP

Long-term:
- Fully embodied AI teacher experience
- Personalized education at scale
- Trustworthy, auditable, effective AI learning

**Build slow. Build clean. Build for mastery.**