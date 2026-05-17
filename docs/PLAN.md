# NexusFlow — Plan Técnico

## Contexto

NexusFlow es una plataforma SaaS de automatización de workflows (estilo Zapier / Make / n8n) pensada como **proyecto de portafolio enterprise-grade**. El objetivo no es solo entregar funcionalidad: es demostrar **diseño de sistemas profesional** — arquitectura modular, patrones .NET reconocibles, separación clara FE/BE, infraestructura reproducible con Docker, y una UI premium al nivel de Linear/Vercel/Stripe.

El MVP debe ser **realista de terminar en solitario** (~6–8 semanas part-time) pero con una arquitectura que se vea seria: nada de atajos que un revisor técnico pueda señalar como amateur (auth pobre, sin migrations, lógica de negocio en controllers, etc.).

**Decisiones ya validadas con el usuario:**

| Área | Decisión |
|---|---|
| Editor de workflows | Lista de pasos en MVP → **React Flow** canvas en Fase 2 (post-MVP) |
| Background jobs | **Hangfire** (dashboard incluido, persistencia en Postgres) |
| Patrón .NET | **Modular Monolith** (módulos: Auth, Workflows, Executions, Integrations) |
| Tiempo real | **SignalR** para logs de ejecución en vivo |
| Tenancy | Multi-tenant con `UserId` scoping (sin Orgs en MVP) |
| Data access | **EF Core 9** code-first + migrations |
| Integraciones | Webhook, Schedule, HTTP, DB, Notification + **Slack + Discord** |
| Deploy | **Vercel** (FE) + **Railway/Fly.io** (BE + Postgres) |

---

## 1. Arquitectura de alto nivel

```
┌─────────────────────────────────────────────────────────────┐
│                    BROWSER (Next.js 15)                     │
│  App Router · React Server Components · shadcn/ui · TW     │
└──────────────────┬──────────────────────────┬───────────────┘
                   │ REST (JWT)               │ WebSocket
                   ▼                          ▼
┌─────────────────────────────────────────────────────────────┐
│              ASP.NET Core 9 — Modular Monolith              │
│  ┌────────┐ ┌──────────┐ ┌────────────┐ ┌──────────────┐   │
│  │  Auth  │ │ Workflows│ │ Executions │ │ Integrations │   │
│  └────────┘ └──────────┘ └────────────┘ └──────────────┘   │
│  ┌──────────────────────────────────────────────────────┐  │
│  │  Shared Kernel: Result<T>, Errors, Auditing, Clock   │  │
│  └──────────────────────────────────────────────────────┘  │
│  ┌─────────────┐  ┌──────────────────────────────────────┐ │
│  │ SignalR Hub │  │ Hangfire (recurring + fire-and-forget)│ │
│  └─────────────┘  └──────────────────────────────────────┘ │
└──────────────────┬──────────────────────────────────────────┘
                   │ EF Core 9
                   ▼
            ┌──────────────┐
            │  PostgreSQL  │  (app schema + hangfire schema)
            └──────────────┘
```

**Principios:**
- Cada módulo expone una **API interna** (interfaces en `Module.Abstractions`); ningún módulo accede a las entidades de otro directamente.
- El motor de ejecución (`Executions`) **consume** la definición de workflow del módulo `Workflows` vía abstracción — listo para extraerse a microservicio si hace falta.
- Triggers/Actions implementan un **contrato común** (`IActionHandler`, `ITriggerSource`) — registrados vía DI con `Scrutor` (assembly scanning).
- Toda llamada a integraciones externas pasa por un **`IHttpClientFactory` tipado** con Polly (retry exponencial + circuit breaker).

---

## 2. Fases del proyecto

### Fase 0 — Fundación (3–4 días)
- Repo monorepo: `/apps/web` (Next.js) + `/apps/api` (ASP.NET) + `/infra` (docker-compose, scripts)
- `docker-compose.yml` con Postgres 16 + pgAdmin opcional
- `Dockerfile` multi-stage para API (.NET 9 SDK → runtime alpine)
- GitHub Actions: `lint + build + test` en PR (mínimo viable, no overkill)
- `README.md` con quickstart (`docker compose up` → todo funcional)
- `.editorconfig`, Prettier, ESLint, .NET analyzers habilitados

**Entregable:** clonar el repo y correr el stack vacío con un comando.

### Fase 1 — Auth + skeleton (4–5 días)
- Módulo **Auth**: registro, login, refresh tokens (rotación), logout
- JWT con claims `sub`, `email`; refresh tokens persistidos en DB (hash bcrypt, expiración, revocación)
- Endpoints: `POST /auth/register`, `/auth/login`, `/auth/refresh`, `/auth/logout`, `GET /auth/me`
- Middleware: `Authorization` policy default `RequireAuthenticatedUser`
- Frontend: pantallas `/login`, `/register`, layout autenticado con guard, Axios interceptor para refresh automático
- Manejo de errores tipados con `ProblemDetails` (RFC 7807)

**Entregable:** flujo de auth completo, refresh transparente, logout invalida refresh token.

### Fase 2 — Modelo de datos + módulo Workflows (3–4 días)
- Diseño del schema (ver sección 5)
- Migrations EF Core iniciales
- CRUD de workflows: crear, listar, obtener, actualizar, eliminar, activar/desactivar
- Validación con FluentValidation
- Repositorios y `IUnitOfWork` por módulo
- Frontend: dashboard `/workflows` con lista + detalle + form de creación (lista de pasos)

**Entregable:** un usuario puede crear, editar y activar/desactivar workflows desde la UI.

### Fase 3 — Motor de ejecución + triggers/actions (7–9 días) [núcleo del proyecto]
- **Triggers:**
  - `WebhookTrigger`: endpoint público `POST /hooks/{workflowId}/{secret}` → encola ejecución
  - `ScheduleTrigger`: registrado en Hangfire `RecurringJob` con expresión cron
- **Actions:**
  - `HttpRequestAction`: GET/POST/PUT/DELETE configurable, soporta templating de variables
  - `SaveToDatabaseAction`: persiste payload en tabla `WorkflowOutputs`
  - `SendNotificationAction`: stub que escribe en log + emite vía SignalR (en Fase 5 conecta a email/Slack real)
- **Engine:** `WorkflowExecutor` recibe `executionId`, recorre los `WorkflowSteps` en orden, mantiene contexto `Dictionary<string, object>` con outputs previos accesibles vía `{{step1.body.field}}`
- **Templating:** mini-motor con Scriban o regex simple para resolución de variables
- Hangfire `BackgroundJob.Enqueue<WorkflowExecutor>(x => x.Run(executionId))`
- Cada step persiste su `StepExecution` con `status`, `input`, `output`, `error`, `durationMs`

**Entregable:** un webhook dispara un workflow que hace HTTP request, guarda en DB y notifica.

### Fase 4 — Dashboard y observabilidad (4–5 días)
- Dashboard principal: stats (workflows activos, ejecuciones últimas 24h, success rate), gráfico simple con Recharts
- Página `/executions`: historial filtrable por workflow, estado, fecha
- Detalle de ejecución: timeline de steps con duración, input/output expandible (JSON viewer)
- Estado visual de workflows (active/inactive/error)
- Empty states bien diseñados

**Entregable:** observabilidad completa, el usuario entiende qué pasó en cada ejecución.

### Fase 5 — SignalR live logs (2–3 días)
- `ExecutionHub` con grupos por `executionId`
- Cliente se suscribe al abrir detalle de ejecución activa → ve steps completándose en vivo
- Reconexión automática con `@microsoft/signalr`
- Fallback a polling si WS no disponible

**Entregable:** ejecutar un workflow y ver los steps actualizándose en tiempo real.

### Fase 6 — Integraciones Slack + Discord (3–4 días)
- Módulo `Integrations` con patrón Provider (`ISlackClient`, `IDiscordClient`)
- Actions: `SlackPostMessageAction`, `DiscordPostMessageAction`
- Credenciales por usuario (webhook URL o token) encriptadas en DB con `IDataProtectionProvider`
- UI: página `/integrations` para conectar/desconectar
- Selector de integración en el formulario de step

**Entregable:** workflow webhook → mensaje en canal de Slack/Discord, funcionando con cuentas reales.

### Fase 7 — Pulido visual premium (4–5 días)
Esta fase es **lo que diferencia el portafolio**. No saltarla.
- Sistema de diseño consolidado: tokens (`--background`, `--foreground`, `--border`, `--accent`), tipografía Inter o Geist
- Dark mode por defecto, light mode opcional con `next-themes`
- Microinteracciones: Framer Motion en transiciones de página, hover states sutiles, skeleton loaders
- Iconografía consistente: `lucide-react` solamente
- Loading states, error states, empty states diseñados (no defaults feos)
- Toasts con `sonner`
- Command palette (⌘K) tipo Raycast con `cmdk` para navegación rápida
- Página landing `/` minimalista estilo Vercel con hero + features + CTA

**Entregable:** captura cualquier pantalla → se ve como un producto SaaS real.

### Fase 8 — Deploy (2–3 días)
- Frontend: Vercel, env vars apuntando al backend de Railway
- Backend: Railway o Fly.io, Dockerfile production-ready
- Postgres: Railway Postgres o Neon (free tier)
- Hangfire dashboard protegido con auth basic
- Variables de entorno documentadas (`.env.example`)
- Dominio custom opcional

**Entregable:** URL pública navegable para mostrar en CV/LinkedIn.

### Fase 9 — Post-MVP (canvas visual)
- Reemplazar lista de pasos por **React Flow** (`@xyflow/react`)
- Nodos custom por tipo de trigger/action
- Persistencia del layout (posiciones) en JSON dentro de la entidad Workflow
- Drag-and-drop desde sidebar de nodos disponibles

---

## 3. Estructura backend (Modular Monolith)

```
apps/api/
├── NexusFlow.Api/                    # Composition root (Program.cs, middlewares)
├── NexusFlow.Shared/                 # Kernel: Result<T>, Error, IClock, IPagedList, BaseEntity
├── Modules/
│   ├── Auth/
│   │   ├── NexusFlow.Auth.Abstractions/      # ICurrentUser, IJwtService (públicas)
│   │   ├── NexusFlow.Auth/                   # Implementación + Endpoints + DbContext
│   │   └── NexusFlow.Auth.Tests/
│   ├── Workflows/
│   │   ├── NexusFlow.Workflows.Abstractions/ # IWorkflowReader (cross-module)
│   │   ├── NexusFlow.Workflows/
│   │   └── NexusFlow.Workflows.Tests/
│   ├── Executions/
│   │   ├── NexusFlow.Executions.Abstractions/
│   │   ├── NexusFlow.Executions/             # Engine, IActionHandler, ITriggerSource
│   │   └── NexusFlow.Executions.Tests/
│   └── Integrations/
│       ├── NexusFlow.Integrations.Abstractions/
│       └── NexusFlow.Integrations/           # Slack, Discord, HTTP clients
└── tests/
    └── NexusFlow.IntegrationTests/           # WebApplicationFactory + Testcontainers
```

**Reglas:**
- Cada módulo tiene su propio `DbContext` apuntando al **mismo Postgres** pero con tablas con prefijo (`auth_users`, `wf_workflows`, `wf_steps`, `exec_executions`, etc.) — facilita extracción futura a microservicios.
- Endpoints con **Minimal APIs** + grupos: `app.MapAuthEndpoints()`, `app.MapWorkflowEndpoints()`. Más limpio que controllers para un proyecto de este tamaño.
- **MediatR** opcional para comandos/queries internos si crece la lógica de un módulo, pero **no obligatorio en MVP** — evitar overhead innecesario.
- **FluentValidation** para validación de DTOs de entrada.
- **Serilog** estructurado → consola + archivo en dev, Seq opcional.
- Filtros globales: `ExceptionToProblemDetailsMiddleware` traduce excepciones a `ProblemDetails`.

---

## 4. Estructura frontend (Next.js 15)

```
apps/web/
├── src/
│   ├── app/
│   │   ├── (auth)/
│   │   │   ├── login/page.tsx
│   │   │   └── register/page.tsx
│   │   ├── (app)/                  # layout protegido con sidebar
│   │   │   ├── layout.tsx
│   │   │   ├── dashboard/page.tsx
│   │   │   ├── workflows/
│   │   │   │   ├── page.tsx
│   │   │   │   ├── new/page.tsx
│   │   │   │   └── [id]/page.tsx
│   │   │   ├── executions/
│   │   │   │   ├── page.tsx
│   │   │   │   └── [id]/page.tsx
│   │   │   └── integrations/page.tsx
│   │   ├── (marketing)/
│   │   │   └── page.tsx            # landing
│   │   └── layout.tsx              # root: theme provider, toaster
│   ├── components/
│   │   ├── ui/                     # shadcn/ui generados
│   │   ├── workflow/               # WorkflowCard, StepEditor, StepList
│   │   ├── execution/              # ExecutionTimeline, StepDetail, LiveLog
│   │   └── shared/                 # Sidebar, CommandPalette, EmptyState
│   ├── lib/
│   │   ├── api/                    # cliente Axios + endpoints tipados
│   │   ├── auth/                   # token storage, refresh logic
│   │   ├── signalr/                # cliente Hub
│   │   └── utils.ts
│   ├── hooks/                      # useWorkflows, useExecution, useLiveLogs
│   ├── stores/                     # Zustand: authStore, uiStore
│   └── types/                      # tipos compartidos generados desde OpenAPI
└── ...
```

**Stack frontend:**
- **TanStack Query** para data fetching y cache (no `fetch` crudo)
- **Zustand** para estado global mínimo (auth, UI flags)
- **react-hook-form** + **zod** para formularios
- **next-themes** para dark mode
- **Tipos generados desde OpenAPI** del backend con `openapi-typescript` (consistencia tipada cross-stack)

---

## 5. Modelo de datos

```
auth_users
├── id (uuid, pk)
├── email (unique)
├── password_hash (bcrypt)
├── created_at, updated_at

auth_refresh_tokens
├── id (uuid, pk)
├── user_id (fk → auth_users)
├── token_hash
├── expires_at
├── revoked_at (nullable)
├── replaced_by_token_id (nullable, fk)

wf_workflows
├── id (uuid, pk)
├── user_id (fk)             # tenancy scoping
├── name, description
├── trigger_type (enum: webhook | schedule)
├── trigger_config (jsonb)   # webhook_secret, cron_expression, etc.
├── is_active (bool)
├── created_at, updated_at

wf_steps
├── id (uuid, pk)
├── workflow_id (fk)
├── order_index (int)
├── action_type (enum: http_request | save_db | send_notification | slack | discord)
├── config (jsonb)           # url, method, body template, channel_id, etc.

exec_executions
├── id (uuid, pk)
├── workflow_id (fk)
├── user_id (fk, denormalized para queries rápidas)
├── status (enum: pending | running | succeeded | failed)
├── triggered_by (enum: webhook | schedule | manual)
├── trigger_payload (jsonb)
├── started_at, finished_at
├── duration_ms (computed)
├── error_message (nullable)

exec_step_executions
├── id (uuid, pk)
├── execution_id (fk)
├── step_id (fk)
├── order_index (int)
├── status, input (jsonb), output (jsonb), error
├── started_at, finished_at, duration_ms

int_user_integrations
├── id (uuid, pk)
├── user_id (fk)
├── provider (enum: slack | discord)
├── credentials_encrypted (text)   # IDataProtectionProvider
├── created_at, revoked_at

wf_workflow_outputs              # para SaveToDatabaseAction
├── id (uuid, pk)
├── workflow_id (fk)
├── execution_id (fk)
├── payload (jsonb)
├── created_at
```

**Índices clave:**
- `wf_workflows(user_id, is_active)`
- `exec_executions(user_id, started_at desc)`
- `exec_executions(workflow_id, status)`
- `exec_step_executions(execution_id, order_index)`

---

## 6. Patrones .NET enterprise a aplicar

| Patrón | Dónde | Por qué |
|---|---|---|
| **Result\<T\>** | Capa Application de cada módulo | Errores tipados sin excepciones para flujo de negocio. Demuestra dominio de F#-style functional C#. |
| **Repository + UnitOfWork** | Acceso a datos por módulo | Aislamiento, testabilidad. Implementados sobre `DbContext`. |
| **Strategy** | `IActionHandler` por tipo de action | Cada action es una clase aislada, registrada por convención. |
| **Factory** | `ITriggerSourceFactory` | Resuelve el trigger source correcto según `trigger_type`. |
| **Decorator** | Logging/timing de action handlers | Vía DI decoration con Scrutor. |
| **Mediator (opcional)** | Comandos complejos | Solo si el módulo lo justifica. |
| **Outbox-light** | Eventos cross-módulo | Tabla `integration_events` + Hangfire poller. **Solo si surge la necesidad** — no en MVP. |
| **Specification** | Queries reutilizables | Para filtros de ejecuciones por estado/fecha. |

**No aplicar en MVP:**
- CQRS con bases separadas (overkill)
- Event sourcing (overkill)
- DDD aggregates estrictos (Workflows tiene 2 entidades — no justifica complejidad)

---

## 7. Diseño visual / sistema UI

**Referencia primaria:** Linear y Vercel Dashboard.

**Tokens (vía CSS variables + Tailwind config):**
```
--background: 0 0% 4%        (zinc-950)
--foreground: 0 0% 98%
--border: 0 0% 12%           (sutil, casi imperceptible)
--accent: 220 90% 56%        (azul eléctrico para CTA)
--muted: 0 0% 9%
--card: 0 0% 6%
```

**Reglas de UI:**
- **Tipografía:** Geist Sans (Vercel) o Inter. Pesos 400/500/600 únicamente.
- **Bordes:** `1px solid hsl(var(--border))`, nunca sombras pesadas. Bordes sutiles >> drop shadows.
- **Spacing:** generoso, basado en escala 4/8/16/24/32. No apretar elementos.
- **Iconografía:** `lucide-react` exclusivamente, tamaño 16px o 20px.
- **Tablas:** filas sin separador visible salvo hover. Headers en `text-muted`.
- **Cards:** `rounded-lg border bg-card`, padding 24px.
- **Buttons:** primario solo para acción principal por pantalla. Secundarios siempre `variant="outline"` o `"ghost"`.
- **Animaciones:** `ease-out duration-150` por defecto. Nada >300ms salvo transiciones de página.
- **Estados vacíos:** ilustración SVG minimalista (o solo icono grande muted) + título + CTA.

**Layout app:**
- Sidebar fijo izquierdo 240px con logo + nav + user menu abajo
- Top bar minimalista con breadcrumb + ⌘K
- Contenido con `max-w-6xl` centrado

---

## 8. Buenas prácticas enterprise a mostrar

- **OpenAPI/Swagger** habilitado en dev, generación automática de tipos TS
- **Migrations versionadas** en repo (no `EnsureCreated`)
- **Tests:**
  - Unit tests de `WorkflowExecutor` con steps mockeados (xUnit + FluentAssertions)
  - Integration tests con **Testcontainers** levantando Postgres real
  - 1–2 tests E2E con Playwright sobre flujos críticos (login + crear workflow + ejecutar)
- **Logging estructurado** con Serilog + contexto enriquecido (`UserId`, `WorkflowId`, `ExecutionId`)
- **Health checks** (`/health/live`, `/health/ready`) con check de Postgres
- **Rate limiting** en endpoints públicos (webhook trigger especialmente) con `AspNetCoreRateLimit` o el built-in de .NET 9
- **CORS** restringido al dominio del frontend
- **Secrets:** nunca en código, `.env` en local + variables en Railway/Vercel en prod
- **Conventional Commits** + CHANGELOG.md generado
- **README profesional** con: badges, demo GIF, arquitectura, quickstart, decisiones técnicas (link a este plan)

---

## 9. Riesgos y mitigaciones

| Riesgo | Mitigación |
|---|---|
| Sub-estimar la Fase 3 (engine) | Empezar con un solo action (HTTP), validar el ciclo end-to-end, luego agregar el resto |
| Pulido visual queda al final y se rushea | Aplicar tokens/spacing desde Fase 1; Fase 7 es refinamiento, no diseño desde cero |
| Hangfire + EF Core sharing context | Usar scopes separados para jobs (`IServiceScopeFactory`) — documentado en Hangfire docs |
| SignalR detrás de Railway con websockets | Verificar soporte WS de Railway en Fase 0; Fly.io es alternativa garantizada |
| Encriptación de credenciales de integraciones | Usar `IDataProtectionProvider` con keyring persistido en Postgres (paquete `AspNetCore.DataProtection.EntityFrameworkCore`) |

---

## 10. Archivos críticos a crear (referencia rápida)

**Backend:**
- `apps/api/NexusFlow.Api/Program.cs` — composition root, registro de módulos
- `apps/api/Modules/Executions/.../WorkflowExecutor.cs` — núcleo del engine
- `apps/api/Modules/Executions/.../IActionHandler.cs` — contrato extensible
- `apps/api/Modules/Auth/.../JwtService.cs` + `RefreshTokenService.cs`
- `apps/api/NexusFlow.Shared/Result.cs` — tipo Result\<T, Error\>

**Frontend:**
- `apps/web/src/lib/api/client.ts` — Axios con interceptors
- `apps/web/src/lib/signalr/executionHub.ts` — cliente Hub
- `apps/web/src/components/workflow/StepEditor.tsx` — editor de pasos MVP
- `apps/web/src/app/(app)/layout.tsx` — shell con sidebar + ⌘K

**Infra:**
- `docker-compose.yml` — Postgres + API + Web
- `apps/api/Dockerfile` — multi-stage build
- `.github/workflows/ci.yml` — build + test en PR

---

## 11. Verificación end-to-end

Al terminar el MVP, este escenario debe correr sin intervención manual:

1. **Setup local:** `git clone && docker compose up` → todo arriba en <60s
2. **Smoke test funcional:**
   - Registrarse en `/register`, login automático
   - Crear workflow "Ping a httpbin" con trigger webhook + 2 steps (HTTP request → save to DB)
   - Activar workflow
   - Copiar webhook URL, hacer `curl -X POST <url>` con body JSON
   - Ver ejecución aparecer en `/executions` con status `succeeded`
   - Abrir detalle, ver timeline de steps con duración y outputs
3. **Smoke test real-time:**
   - Crear workflow con `Schedule` cada 1 min + 3 steps con sleeps simulados
   - Abrir detalle de ejecución activa, ver steps completándose en vivo vía SignalR
4. **Smoke test integración:**
   - Conectar Slack en `/integrations`
   - Workflow webhook → `SlackPostMessageAction` con `{{trigger.body.message}}`
   - POST al webhook → mensaje aparece en canal de Slack
5. **Test deploy:**
   - Push a `main` → Vercel deploya frontend
   - Railway redeploya backend
   - Mismo smoke test funciona contra URLs públicas
6. **Tests automatizados:**
   - `dotnet test` en CI → todos verdes
   - `pnpm test` (Playwright) → flujos críticos verdes

---

## 12. Timeline realista

| Fase | Duración (part-time, ~15h/sem) |
|---|---|
| 0 — Fundación | 3–4 días |
| 1 — Auth | 4–5 días |
| 2 — Workflows CRUD | 3–4 días |
| 3 — Engine + triggers/actions | 7–9 días |
| 4 — Dashboard + observabilidad | 4–5 días |
| 5 — SignalR | 2–3 días |
| 6 — Slack/Discord | 3–4 días |
| 7 — Pulido visual | 4–5 días |
| 8 — Deploy | 2–3 días |
| **Total MVP** | **~6–8 semanas** |
| 9 — React Flow canvas (post-MVP) | 1–2 semanas extra |

---

## Resumen ejecutivo

NexusFlow se construye como un **Modular Monolith .NET 9** con UI Next.js 15 premium, ejecución de workflows via Hangfire, logs en vivo con SignalR, y un MVP funcional con 5 actions + 2 integraciones reales (Slack/Discord). La fase de pulido visual (Fase 7) es **no negociable** para el objetivo de portafolio. Deploy a Vercel + Railway permite URL pública sin costo significativo. La arquitectura está preparada para evolucionar (canvas visual en Fase 9, extracción a microservicios si crece) sin pagar la complejidad por adelantado.
