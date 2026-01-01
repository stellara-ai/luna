# Luna — AI-Powered Homeschool & Tutoring Platform

> A live, adaptive, conversational AI teacher bringing the best teachers into the home.

## Quick Start

### Prerequisites
- .NET 10 LTS
- Node.js 20+
- Docker & Docker Compose
- PostgreSQL 15+ (or via Docker)

### Local Development

```bash
# Clone the repository
git clone <repo> && cd luna

# Start local services (PostgreSQL, Redis, etc.)
docker-compose -f docker/compose.local.yml up -d

# Backend
cd src/backend
dotnet restore
dotnet build
dotnet run --project apps/Luna.ApiGateway

# Frontend (new terminal)
cd src/frontend/luna-web
npm install
npm run dev
```

Visit `http://localhost:5173` (frontend) and `http://localhost:5000/api` (backend).

---

## Architecture

Luna is a **modular monolith** with hard module boundaries, designed to become microservices later.

See [docs/architecture/LUNA_CONTEXT.md](docs/architecture/LUNA_CONTEXT.md) for the full architectural vision.

### Core Modules
- **Luna.Identity** — Authentication & user identity
- **Luna.Classroom** — Real-time teaching sessions (core domain)
- **Luna.Students** — Student profiles & learning preferences
- **Luna.Curriculum** — Lesson definitions & strategies
- **Luna.Media** — TTS/STT provider abstraction

---

## Development

### Backend
- .NET 10 LTS + ASP.NET Core Minimal APIs
- WebSockets for real-time sessions
- PostgreSQL (per-module schemas)
- Redis for session state

### Frontend
- Vue 3 + TypeScript + Vite
- Voice-first UX (TTS now, STT next)
- ADHD-friendly UI patterns

---

## File Structure

```
luna/
├── docs/architecture/         # Design decisions
├── docker/                    # Local dev compose files
├── infra/                     # Infrastructure (Terraform, localstack)
└── src/
    ├── backend/               # .NET services
    │   ├── shared/            # Contracts & shared kernel
    │   ├── services/          # Domain modules
    │   ├── apps/              # Luna.ApiGateway (entry point)
    │   └── tests/             # Unit & integration tests
    └── frontend/              # Vue 3 application
```

---

## Key Principles

✅ **Modular Monolith** — One deployable host, clear module boundaries  
✅ **Microservices Ready** — Each module can become its own service  
✅ **Observable & Auditable** — All events logged, all AI output inspectable  
✅ **Safety First** — Built for minors (parent visibility, kill-switch, content boundaries)  
✅ **Learning Effectiveness** — Not novelty, but mastery  

---

## License

This project is proprietary software owned by Stellara AI LLC.  
All rights reserved.
