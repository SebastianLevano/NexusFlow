<div align="center">

# NexusFlow

**A portfolio-grade SaaS workflow automation platform — Zapier/Make/n8n in spirit, built end-to-end in .NET 9 + Next.js 15.**

[![CI](https://github.com/SebastianLevano/NexusFlow/actions/workflows/ci.yml/badge.svg)](https://github.com/SebastianLevano/NexusFlow/actions/workflows/ci.yml)
![.NET 9](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet&logoColor=white)
![Next.js 15](https://img.shields.io/badge/Next.js-15-000000?logo=next.js&logoColor=white)
![TypeScript](https://img.shields.io/badge/TypeScript-strict-3178C6?logo=typescript&logoColor=white)
![PostgreSQL 16](https://img.shields.io/badge/PostgreSQL-16-4169E1?logo=postgresql&logoColor=white)
![License: MIT](https://img.shields.io/badge/license-MIT-green.svg)

### 🚀 [**Live demo**](https://nexus-flow-henna.vercel.app) · 📘 [Technical plan](docs/PLAN.md) · 🛠 [Deploy guide](docs/DEPLOY.md)

</div>

> **⏱ First-load disclaimer:** the demo backend runs on Render's free tier and **sleeps after 15 min of inactivity**. The very first request after a quiet period wakes the container — expect a **~30–50 s cold start the first time you open the live demo**, then it runs instantly. Register a fresh user with any email/password (8+ chars) — accounts are isolated per user.

---

## What it does

NexusFlow lets a user automate work across services using a **trigger → step → step …** model, the same way Zapier or n8n do, but built end-to-end as a single coherent codebase you can read in an afternoon.

- ⚡ **Triggers**: incoming webhooks (with shared secret), cron schedules, or manual one-off runs from the UI.
- 🧩 **Actions**: HTTP requests, save-to-database, in-app notifications, and **real Slack + Discord integrations** via incoming webhooks (credentials encrypted at rest).
- 🔁 **Templating**: pass data between steps with `{{ trigger.body.field }}` or `{{ step1.output.field }}` — types are preserved when the whole string is a single expression.
- 📡 **Live observability**: a dashboard with stats, a timeseries chart, and per-execution timelines that **stream step transitions over SignalR** while a workflow is running.
- 🔐 **Multi-tenant from day one**: every query is scoped by `UserId`, every secret is encrypted, every refresh token is single-use and rotated.

---

## Try the demo in 90 seconds

1. Open [**the live demo**](https://nexus-flow-henna.vercel.app) and **register** with any email + 8-char password.
2. **Workflows → + New workflow**:
   - Trigger: `webhook` with config `{"secret": "abc"}`
   - Step: `http_request` with config
     ```json
     {"method":"POST","url":"https://httpbin.org/post","body":{"echo":"{{ trigger.body.message }}"}}
     ```
3. **Activate** the workflow. The detail page shows a copyable **Webhook URL**.
4. From a terminal, fire the webhook with your URL:
   ```bash
   curl -X POST "<webhook-url>" -H "Content-Type: application/json" \
     -d '{"message":"hello from prod"}'
   ```
5. **Executions** → click the new run → watch the step go `pending → running → succeeded` **in real time** (SignalR), with input/output JSON expandable inline.

Press **⌘K** anywhere in the app for a Raycast-style command palette.

---

## Highlights for reviewers

Specific design decisions you'll find in the code:

| Area | What's interesting |
|---|---|
| **Architecture** | Modular Monolith — 4 modules (Auth · Workflows · Executions · Integrations), each with `*.Abstractions` + impl. No module references another module's impl. Ready to extract to microservices without rewriting. |
| **Result\<T\> pattern** | Errors are typed values, not exceptions, for business flow. Implicit conversion `Error → Result<T>` keeps the surface clean. RFC 7807 `ProblemDetails` on the wire. |
| **Auth** | JWT access tokens + refresh-token rotation (SHA-256 hashed at rest, single-use, `replaced_by_token_id` chain to detect reuse). BCrypt 12. JWT signing key validated at startup. |
| **Execution engine** | `WorkflowExecutor` orchestrates over `IActionHandler` strategy; templating engine preserves typed values; persisted per-step transitions (input/output/duration/error). |
| **Background jobs** | Hangfire over Postgres for fire-and-forget + recurring jobs. Schedules re-registered on startup so cron survives restarts. |
| **SignalR cross-module** | The executor publishes events through `IExecutionLiveBus` in `Executions.Abstractions`. The SignalR hub implements that contract — swap for Kafka/Redis Streams without touching the executor. |
| **Encrypted credentials** | Slack/Discord webhook URLs encrypted via `IDataProtectionProvider` (purpose-scoped). Keys persisted to a volume so they survive restarts. |
| **EF Core 9** | Code-first, **per-module schemas** (`auth`, `workflows`, `executions`, `integrations`), per-module `__ef_migrations_history`. Auto-migrate on startup, skipped under `Testing`. |
| **Connection string normalizer** | Detects libpq URI form (`postgresql://user:pass@host/db`) — used by Render/Neon/Supabase/Heroku/Railway — and rewrites to Npgsql keyword form at boot. Paste the provider's URL as-is. |
| **Liveness vs readiness** | `/health/live` proves the process is alive; `/health/ready` runs the Postgres check. Splits the two so orchestrators don't restart on transient DB hiccups. |
| **Frontend** | Next.js 15 App Router · `typedRoutes` · Zustand single-flight refresh interceptor · Recharts dashboard · cmdk command palette · Framer Motion transitions · next-themes dark/light · SignalR client with auto-reconnect + polling fallback. |
| **Testing** | xUnit + Testcontainers spin up a real Postgres for integration tests covering the full auth + workflows lifecycle. CI builds API + web on every push. |

---

## Tech stack

| Layer | Stack |
|---|---|
| Frontend | Next.js 15 · TypeScript (strict) · Tailwind · shadcn/ui · Recharts · cmdk · Framer Motion · @microsoft/signalr |
| Backend  | ASP.NET Core 9 · Minimal APIs · Modular Monolith · EF Core 9 · FluentValidation · Serilog |
| Database | PostgreSQL 16 (per-module schemas) |
| Jobs     | Hangfire (Postgres-backed) |
| Realtime | SignalR (WSS behind proxy) |
| Auth     | JWT + refresh-token rotation, multi-tenant via `UserId` scoping, BCrypt 12 |
| Secrets  | `IDataProtectionProvider` (filesystem-persisted keyring) |
| Infra    | Docker · Docker Compose · GitHub Actions CI |
| Deploy   | Vercel (web) · Render (api + Postgres) — full guide in [`docs/DEPLOY.md`](docs/DEPLOY.md) |

---

## Repository layout

```
NexusFlow/
├── apps/
│   ├── web/                # Next.js 15 frontend (App Router)
│   └── api/                # ASP.NET Core 9 modular monolith
│       ├── NexusFlow.Api/             # composition root (Program.cs)
│       ├── NexusFlow.Shared/          # Result<T>, IClock, BaseEntity
│       ├── Modules/
│       │   ├── Auth/{Abstractions,Auth}
│       │   ├── Workflows/{Abstractions,Workflows}
│       │   ├── Executions/{Abstractions,Executions}    # engine + SignalR hub
│       │   └── Integrations/{Abstractions,Integrations} # Slack + Discord
│       └── tests/NexusFlow.IntegrationTests             # xUnit + Testcontainers
├── infra/
│   └── docker-compose.yml  # postgres + api + web (+ optional pgadmin)
├── docs/
│   ├── PLAN.md             # full 9-phase technical plan
│   └── DEPLOY.md           # step-by-step deploy guide
└── .github/workflows/ci.yml
```

Each backend module ships **two projects**: `*.Abstractions` (interfaces consumed cross-module) and the implementation. No module references another module's implementation — only its abstractions. That boundary is what makes an eventual microservice extraction cheap.

---

## Run locally

### Prerequisites
- Docker Desktop or Docker Engine 24+
- _(Optional, for dev outside Docker)_ .NET 9 SDK, Node.js 20+

### One command

```bash
cp .env.example .env
docker compose up --build
```

When the stack is up:

| Service | URL |
|---|---|
| Web (Next.js) | http://localhost:3000 |
| API | http://localhost:5080 |
| API Swagger | http://localhost:5080/swagger |
| Hangfire dashboard | http://localhost:5080/hangfire (open access in Development) |
| Module health | http://localhost:5080/{auth,workflows,executions,integrations}/health |
| Postgres | localhost:5433 (user: `nexusflow`, db: `nexusflow`) |
| pgAdmin (optional) | http://localhost:5050 — start with `docker compose --profile tools up` |

### Dev outside Docker

```bash
# API
cd apps/api && dotnet run --project NexusFlow.Api

# Web
cd apps/web && npm install && npm run dev
```

---

## Deploy

This live demo runs on **Vercel (frontend) + Render (API + Postgres)**, all on free tiers. Full step-by-step in [`docs/DEPLOY.md`](docs/DEPLOY.md), including:

- Provider env vars (the Postgres URL normalizer means you paste it as-is)
- Hangfire dashboard credentials
- CORS / SignalR behind HTTPS proxy
- Custom domain notes
- A Fly.io alternative

---

## Conventions

- **.NET**: `TreatWarningsAsErrors=true`, `Nullable=enable`, latest analyzers, central package management. Errors as `Result<T>` internally, ProblemDetails on the wire.
- **TypeScript**: strict mode, no `any`, Prettier + Tailwind plugin. URL-as-state for filter pages.
- **Tests**: xUnit + FluentAssertions for unit; Testcontainers + `WebApplicationFactory` for integration. CI on every push.
- **Commits**: Conventional Commits (`feat:`, `fix:`, `chore:`, …).

---

## What's next (post-MVP)

- **React Flow canvas** to replace the linear step editor — drag-and-drop nodes with custom shapes per action type. Stored as a layout JSON next to the existing `wf_steps` rows (no destructive schema change).
- **More integrations**: Telegram, Microsoft Teams, generic OAuth provider.
- **Outbox-light** for cross-module events (when justified).
- **Migrate the free Render Postgres** to Neon before day 90 — the free Render tier auto-deletes.

---

## License

MIT
