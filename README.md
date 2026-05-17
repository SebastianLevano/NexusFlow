# NexusFlow

Modern workflow automation platform — Zapier/Make/n8n-style — built as a portfolio-grade SaaS.

> **Status:** Phase 3 — Execution engine complete. Workflows run end-to-end: webhook/manual/schedule triggers enqueue background jobs via Hangfire, the `WorkflowExecutor` steps through ordered actions (HTTP / save / notification) with variable templating, and the UI shows live execution timelines.

---

## Tech stack

| Layer | Stack |
|---|---|
| Frontend | Next.js 15 · TypeScript · Tailwind · shadcn/ui · next-themes |
| Backend  | ASP.NET Core 9 · Minimal APIs · Modular Monolith · EF Core 9 |
| Database | PostgreSQL 16 |
| Jobs     | Hangfire (Postgres-backed) |
| Realtime | SignalR (added in Phase 5) |
| Auth     | JWT + refresh-token rotation, multi-tenant via `UserId` scoping |
| Infra    | Docker · Docker Compose · GitHub Actions CI |
| Deploy   | Vercel (web) · Railway/Fly.io (api + db) |

The full technical plan lives in [`docs/PLAN.md`](docs/PLAN.md) (mirror of the approved plan file).

---

## Repository layout

```
NexusFlow/
├── apps/
│   ├── web/                # Next.js 15 frontend
│   └── api/                # ASP.NET Core 9 modular monolith
│       ├── NexusFlow.Api/             # composition root
│       ├── NexusFlow.Shared/          # Result<T>, IClock, BaseEntity
│       ├── Modules/
│       │   ├── Auth/{Abstractions,Auth}
│       │   ├── Workflows/{Abstractions,Workflows}
│       │   ├── Executions/{Abstractions,Executions}
│       │   └── Integrations/{Abstractions,Integrations}
│       └── tests/NexusFlow.IntegrationTests
├── infra/
│   └── docker-compose.yml  # postgres + api + web (+ optional pgadmin)
├── .github/workflows/ci.yml
└── docker-compose.yml      # shim → infra/docker-compose.yml
```

Each backend module ships **two projects**: `*.Abstractions` (interfaces consumed cross-module) and the implementation. No module references another module's implementation — only its abstractions. This is the boundary that lets us extract to microservices later if needed.

---

## Quickstart

### Prerequisites
- Docker Desktop or Docker Engine 24+
- (Optional, for local dev outside Docker) .NET 9 SDK, Node.js 20+

### Run everything

```bash
cp .env.example .env
docker compose up --build
```

When the stack is up:

| Service | URL |
|---|---|
| Web (Next.js) | http://localhost:3000 |
| API root | http://localhost:5080 |
| API Swagger | http://localhost:5080/swagger |
| Module health | http://localhost:5080/{auth,workflows,executions,integrations}/health |
| Postgres | localhost:5432 (user: `nexusflow`, db: `nexusflow`) |
| pgAdmin (optional) | http://localhost:5050 — start with `docker compose --profile tools up` |

### Local dev (without Docker)

**API:**
```bash
cd apps/api
dotnet restore
dotnet run --project NexusFlow.Api
```

**Web:**
```bash
cd apps/web
npm install
npm run dev
```

---

## What's already in place

### Foundation (Phase 0)
- [x] Monorepo layout (`apps/web`, `apps/api`, `infra/`)
- [x] ASP.NET Core 9 modular monolith with 4 modules registered and routed
- [x] `Result<T>` / `Error` types in the shared kernel, with HTTP/ProblemDetails translation
- [x] Centralized package versions via `Directory.Packages.props`
- [x] `TreatWarningsAsErrors`, .NET analyzers, nullable everywhere
- [x] Serilog structured logging
- [x] Health checks (`/health/live`, `/health/ready`) + per-module health
- [x] CORS configured from `Cors:AllowedOrigins`
- [x] Swagger/OpenAPI in Development
- [x] Multi-stage Dockerfiles (alpine runtime for both apps)
- [x] Docker Compose with healthcheck-aware dependency on Postgres
- [x] GitHub Actions CI: API build+test, web lint+typecheck+build
- [x] Tailwind tokens aligned with Linear/Vercel aesthetic
- [x] xUnit + Testcontainers wired against `WebApplicationFactory`

### Auth (Phase 1)
- [x] `User` + `RefreshToken` domain entities under the Auth module
- [x] EF Core 9 `AuthDbContext` + initial migration (`__ef_migrations_history` namespaced per module)
- [x] **Auto-migrate on startup** (skipped under `ASPNETCORE_ENVIRONMENT=Testing`)
- [x] BCrypt password hashing (work factor 12)
- [x] JWT issuance (`IJwtService`) with `Sub` / `Email` / `Jti` claims
- [x] Refresh tokens: SHA-256 hashed at rest, 64-byte random, rotation on every refresh, single-use enforced via `replaced_by_token_id`
- [x] `IRefreshTokenService` with rotation + revocation chain
- [x] JWT auth middleware + fallback `RequireAuthenticatedUser` policy
- [x] `ICurrentUser` exposed via `HttpContextAccessor`
- [x] Endpoints: `POST /auth/{register,login,refresh,logout}` + `GET /auth/me`
- [x] FluentValidation request validators wired through a generic `ValidationFilter<T>`
- [x] Strongly-typed errors via RFC 7807 `ProblemDetails` (`detail`, `title`, `code`)
- [x] Integration tests covering the full lifecycle + rejection of reused/revoked tokens
- [x] Next.js Axios client with **transparent refresh** (single-flight, redirects to `/login` on failure)
- [x] Zustand `auth-store` (register / login / logout / hydrate from token storage)
- [x] `/login` and `/register` pages with `react-hook-form` + `zod`
- [x] Protected `(app)` layout with `AuthGuard`, premium sidebar, and `/dashboard` placeholder
- [x] Sonner toasts for auth errors

### Workflows (Phase 2)
- [x] `Workflow` + `WorkflowStep` domain entities with rich behavior (`Update`, `Activate`/`Deactivate`, `ReplaceSteps`)
- [x] `WorkflowsDbContext` with `jsonb` columns for trigger/step configs and a unique `(workflow_id, order_index)` index
- [x] Initial migration (`__ef_migrations_history` namespaced under the `workflows` schema)
- [x] `IWorkflowReader` cross-module abstraction exposed via `NexusFlow.Workflows.Abstractions`
- [x] Endpoints: `GET/POST /workflows`, `GET/PUT/DELETE /workflows/{id}`, `POST /workflows/{id}/{activate,deactivate}`
- [x] Tenancy: every query scoped by `ICurrentUser.UserId` → cross-tenant access returns `404`
- [x] FluentValidation for create/update + `JsonElement` config payloads validated to be JSON objects
- [x] Integration tests: full CRUD lifecycle, cross-tenant isolation, anonymous → 401
- [x] Premium UI: `/workflows` list with empty state, `WorkflowCard` with hover actions (toggle/delete), `/workflows/new` and `/workflows/[id]` with `StepEditor` (reorder, JSON config, per-action samples)
- [x] Form layer: react-hook-form + zod (transform validates JSON to object) + Sonner toasts for success/error

### Execution engine (Phase 3)
- [x] `Execution` + `StepExecution` + `WorkflowOutput` domain entities under the Executions module
- [x] EF Core `ExecutionsDbContext` + migration (`__ef_migrations_history` namespaced under `executions`)
- [x] Templating engine (`{{ trigger.body.x }}`, `{{ step1.output.field }}`) — preserves typed values when the whole string is one template
- [x] `IActionHandler` strategy contract + 3 real handlers (`http_request`, `save_to_database`, `send_notification`) + 2 stubs (Slack/Discord land in Phase 6)
- [x] `WorkflowExecutor` orchestrator: persists `pending → running → succeeded/failed` transitions per execution and per step, captures input/output/duration
- [x] Hangfire (Postgres-backed) wired with workers, `IBackgroundJobClient` for fire-and-forget and `IRecurringJobManager` for cron schedules
- [x] `IExecutionScheduler` (cross-module) creates pending execution + enqueues `WorkflowExecutor.RunAsync`
- [x] `IScheduleRegistrar` (cross-module) registers/removes Hangfire recurring jobs when workflows are activated/deactivated/updated/deleted
- [x] Public **webhook endpoint** `POST /hooks/{workflowId}/{secret}` — validates secret, accepts JSON payload, enqueues
- [x] Authenticated **manual trigger** `POST /workflows/{id}/runs`
- [x] `GET /executions[?workflowId=...]` and `GET /executions/{id}` (scoped by `UserId`)
- [x] Schedules re-registered on app startup so cron jobs survive restarts
- [x] Hangfire dashboard at `/hangfire` (dev-only auth filter)
- [x] UI: `/executions` list with auto-refresh (5s), execution detail with **step timeline** (input/output/error per step, JSON expandable), live-polling while running
- [x] UI: workflow detail now shows **Run now**, **View runs**, and a **copyable webhook URL** when the trigger is a webhook

## Deploy

Production-ready. See **[`docs/DEPLOY.md`](docs/DEPLOY.md)** for a step-by-step guide targeting:

- **Vercel** (frontend) — `apps/web` deploys as a Next.js project
- **Railway** (backend + Postgres) — `apps/api/Dockerfile` ships ready to deploy; managed Postgres plugin handles the database; persistent volume holds DataProtection keys
- **Fly.io** as an alternative for the backend

Includes secret rotation, custom domain setup, Hangfire dashboard basic-auth credentials, and a smoke-test checklist.

## Upcoming phases

See `docs/PLAN.md` for the full 9-phase plan. Post-MVP:

- **Phase 9** — React Flow visual canvas to replace the linear step editor

---

## Development conventions

- **Conventional Commits** (`feat:`, `fix:`, `chore:`, `refactor:`, `docs:`, `test:`)
- **.NET:** `TreatWarningsAsErrors=true`, `Nullable=enable`, latest analyzers, central package management
- **TypeScript:** strict mode, no `any`, Prettier + Tailwind plugin formatting
- **Tests:** xUnit + FluentAssertions for unit; Testcontainers + `WebApplicationFactory` for integration; Playwright for E2E (Phase 4+)
- **Errors:** RFC 7807 ProblemDetails on the wire; `Result<T>` internally — no exceptions for business flow

---

## License

MIT
