# Luna

**Luna** is a production-grade, AI-powered tutoring and homeschool platform designed to deliver a **live, adaptive virtual teacher** into the home.

Luna is built **voice-first**, optimized for neurodivergent learners (starting with ADD / ADHD), and focused on *real understanding* rather than content delivery. The system adapts pacing, explanation style, analogies, and reinforcement dynamically based on each student’s needs.

---

## Vision

The long-term goal of Luna is to create a **live AI teacher** that teaches like a great human tutor — using real-time conversation, cadence, pauses, visual explanations, and continuous adaptation to the student’s learning style and attention.

Luna prioritizes learning effectiveness, trust, and safety over novelty.

---

## Architecture Overview

- **Backend:** .NET 10 LTS, ASP.NET Core
- **Frontend:** Vue 3 + TypeScript
- **Realtime:** WebSockets (voice-first interaction)
- **Data:** PostgreSQL, Redis
- **Infrastructure:** Docker, AWS (ECS Fargate)
- **Observability:** OpenTelemetry

The backend is intentionally designed as a **modular monolith** with clear domain boundaries and versioned contracts, making it **microservices-ready** without premature complexity.

---

## Core Domains

- **Identity** — authentication and tenant isolation
- **Classroom** — real-time teaching sessions and orchestration
- **Students** — learner profiles and accommodations
- **Curriculum** — lessons, subjects, and pedagogy
- **Media** — text-to-speech and speech-to-text providers

---

## Current Status

Early development  
Initial focus: one student, one subject, voice-first tutoring  
Teaching intelligence and session orchestration are the core priorities

---

## Documentation

Key architectural decisions and project context live in:
