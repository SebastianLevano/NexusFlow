# Deploying NexusFlow

This guide walks through deploying NexusFlow to **Vercel** (frontend) + **Railway** (backend + Postgres). The same backend setup works on Fly.io or any host that runs Docker.

> **Cost:** with the recommended providers you can run a personal/portfolio instance on free tiers (Vercel free, Railway free $5/mo credit, or Neon free Postgres). Custom domains are optional.

---

## 1. Prerequisites

- A GitHub account with this repo pushed (Vercel + Railway pull from GitHub).
- A Vercel account ([vercel.com](https://vercel.com)).
- A Railway account ([railway.app](https://railway.app)) — or alternatively Fly.io.
- (Optional) A Slack/Discord workspace if you want to test the integrations live.

Generate a strong JWT signing key now — you'll paste it later:

```bash
openssl rand -base64 48
```

---

## 2. Deploy the backend on Railway

1. From the Railway dashboard click **New Project** → **Deploy from GitHub repo** → pick this repository.
2. Railway autodetects the **`apps/api/Dockerfile`**. If it doesn't:
   - Settings → Source → **Root Directory**: `apps/api`
   - Settings → Build → **Build Method**: Dockerfile
3. Add a **PostgreSQL** plugin to the same project (Railway dashboard → **+ New** → **Database** → **PostgreSQL**).
4. In the API service → **Variables** tab, paste this (replacing values):

   ```
   ASPNETCORE_ENVIRONMENT=Production
   ASPNETCORE_URLS=http://+:8080
   PORT=8080
   ConnectionStrings__Postgres=${{Postgres.DATABASE_URL}}
   Jwt__Issuer=nexusflow
   Jwt__Audience=nexusflow-web
   Jwt__SigningKey=<paste output of openssl rand -base64 48>
   Jwt__AccessTokenMinutes=15
   Jwt__RefreshTokenDays=14
   Cors__AllowedOrigins=https://<your-vercel-domain>.vercel.app
   Hangfire__DashboardUser=admin
   Hangfire__DashboardPassword=<a-strong-password>
   DataProtection__KeysPath=/app/keys
   ```

   - The `${{Postgres.DATABASE_URL}}` syntax pulls the connection string from the linked Postgres plugin. If Railway gives you a libpq URL (`postgres://user:pass@host:5432/db`), it works with EF Core's Npgsql provider as-is.
   - Leave `Cors__AllowedOrigins` empty for now if you don't have the Vercel URL yet — come back after step 3.

5. **Persistent volume** for DataProtection keys (so encrypted Slack/Discord credentials survive restarts):
   - Service → Settings → **Volumes** → **+ Add volume**
   - Mount path: `/app/keys`
   - Size: 1 GB is plenty.

6. Settings → Networking → **Generate Domain** to get a public URL (`https://nexusflow-api-production-xxxx.up.railway.app`).

7. Verify:
   ```bash
   curl https://your-api.up.railway.app/health/ready
   # → 200 with no body
   ```

> **Migrations run automatically on startup** (`app.MigrateAuthAsync/...Async`), so the DB schema is created the first time the API boots. Cron schedules are also re-registered then.

---

## 3. Deploy the frontend on Vercel

1. Vercel dashboard → **Add New… Project** → pick this repo.
2. **Root Directory**: `apps/web`
3. Framework: **Next.js** (autodetected).
4. **Environment Variables** (Production scope):

   ```
   NEXT_PUBLIC_API_URL=https://your-api.up.railway.app
   ```

5. Click **Deploy**. After ~2 min you'll get a `https://your-project.vercel.app` URL.
6. Go back to Railway → API service → Variables → update `Cors__AllowedOrigins` to include the Vercel URL. Railway will redeploy automatically.

---

## 4. Verify end-to-end

1. Open `https://your-project.vercel.app`.
2. Register a user — should redirect to the dashboard.
3. Create a workflow with a webhook trigger + an HTTP request step.
4. Activate it, copy the webhook URL (notice it now points to your **Railway** domain, not localhost).
5. Trigger it:
   ```bash
   curl -X POST "https://your-api.up.railway.app/hooks/<workflowId>/<secret>" \
     -H "Content-Type: application/json" -d '{"message":"prod live"}'
   ```
6. `/executions` should show the run, with live SignalR updates working over WSS.

---

## 5. Hangfire dashboard in production

- URL: `https://your-api.up.railway.app/hangfire`
- Authenticated via HTTP Basic with the user/password set in `Hangfire__DashboardUser`/`Hangfire__DashboardPassword`.
- If either is empty, the dashboard is **denied for everyone** in non-Development environments (safe default).

---

## 6. Notes & gotchas

### CORS
Multiple origins are comma-separated:
```
Cors__AllowedOrigins=https://nexusflow.vercel.app,https://www.nexusflow.io
```
You **must** include the exact scheme + host (no trailing slash).

### SignalR behind a proxy
Both Vercel and Railway proxy via HTTPS, so the client connects to `wss://your-api/hubs/executions`. The backend already trusts `X-Forwarded-Proto` (configured in `Program.cs` via `app.UseForwardedHeaders()`), so request scheme detection is correct.

If WebSockets don't connect, the client automatically falls back to LongPolling (already configured in `lib/signalr/execution-hub.ts`).

### DataProtection keys
Stored at `/app/keys` (mounted volume on Railway). If you ever wipe the volume, **all encrypted integration credentials become unrecoverable** — users will need to re-paste their Slack/Discord webhook URLs. This is the same threat model as losing a JWT signing key.

### Custom domain
- **Web**: Vercel project → Settings → Domains → add your domain → follow DNS instructions.
- **API**: Railway service → Settings → Networking → Custom Domain → CNAME the domain. Update `Cors__AllowedOrigins` and `NEXT_PUBLIC_API_URL` accordingly.

### Rotating secrets
- `Jwt__SigningKey` rotation invalidates all active access tokens (users get logged out at next request). Refresh tokens keep working as long as they're in DB.
- `Hangfire__DashboardPassword` rotation has no user impact.

---

## 7. Alternative: Fly.io

If you prefer Fly:

```bash
cd apps/api
fly launch --no-deploy             # creates fly.toml from the Dockerfile
fly secrets set Jwt__SigningKey=... ConnectionStrings__Postgres=...
fly volumes create dpkeys --size 1 --region <your-region>
# In fly.toml add:
#   [mounts] source="dpkeys" destination="/app/keys"
fly deploy
```

Then use `fly postgres create` for managed Postgres, or point to an external Neon/Supabase Postgres.

---

## 8. Local smoke test against production

Before announcing the URL, run the canonical smoke test from your laptop:

```bash
# Register
curl -X POST https://your-api/auth/register -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","password":"correcthorsebatterystaple"}'

# Health
curl https://your-api/health/ready

# Hangfire dashboard (will prompt for Basic auth)
open https://your-api/hangfire
```

All green → ready to share.
