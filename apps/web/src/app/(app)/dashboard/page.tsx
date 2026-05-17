"use client";

import { useCallback, useEffect, useState } from "react";
import Link from "next/link";
import { Activity, CheckCircle2, Clock, History, Workflow, XCircle, Zap } from "lucide-react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { EmptyState } from "@/components/shared/empty-state";
import { ExecutionsChart } from "@/components/dashboard/executions-chart";
import { StatCard } from "@/components/dashboard/stat-card";
import { executionsApi } from "@/lib/executions/api";
import type { ExecutionStats, ExecutionTimeseriesResponse } from "@/lib/executions/types";
import { useAuthStore } from "@/stores/auth-store";

const RANGES = ["24h", "7d"] as const;
type Range = (typeof RANGES)[number];

export default function DashboardPage() {
  const user = useAuthStore((s) => s.user);
  const [stats, setStats] = useState<ExecutionStats | null>(null);
  const [series, setSeries] = useState<ExecutionTimeseriesResponse | null>(null);
  const [range, setRange] = useState<Range>("24h");
  const [error, setError] = useState(false);

  const load = useCallback(async (r: Range) => {
    try {
      const [s, t] = await Promise.all([executionsApi.stats(), executionsApi.timeseries(r)]);
      setStats(s);
      setSeries(t);
      setError(false);
    } catch {
      setError(true);
      toast.error("Could not load dashboard stats.");
    }
  }, []);

  useEffect(() => {
    void load(range);
    const t = window.setInterval(() => void load(range), 15000);
    return () => window.clearInterval(t);
  }, [load, range]);

  const successRatePct =
    stats && (stats.succeededLast24h + stats.failedLast24h) > 0
      ? `${(stats.successRateLast24h * 100).toFixed(1)}%`
      : "—";

  return (
    <div className="space-y-8">
      <header className="flex flex-wrap items-end justify-between gap-4">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight">Welcome back</h1>
          <p className="mt-1 text-sm text-muted-foreground">
            Signed in as <span className="text-foreground">{user?.email}</span>
          </p>
        </div>
        <div className="flex items-center gap-2">
          <Button asChild>
            <Link href="/workflows/new">
              <Zap className="h-3.5 w-3.5" />
              New workflow
            </Link>
          </Button>
          <Button asChild variant="outline">
            <Link href="/executions">
              <History className="h-3.5 w-3.5" />
              View runs
            </Link>
          </Button>
        </div>
      </header>

      <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
        <StatCard
          icon={<Workflow className="h-4 w-4" />}
          label="Active workflows"
          value={stats ? String(stats.workflowsActive) : "—"}
          hint={stats ? `${stats.workflowsTotal} total` : undefined}
          loading={!stats}
        />
        <StatCard
          icon={<Activity className="h-4 w-4" />}
          label="Executions (24h)"
          value={stats ? String(stats.runs24h) : "—"}
          hint={stats ? `${stats.runs7d} in 7d` : undefined}
          loading={!stats}
        />
        <StatCard
          icon={<CheckCircle2 className="h-4 w-4" />}
          label="Success rate (24h)"
          value={successRatePct}
          hint={
            stats && (stats.succeededLast24h + stats.failedLast24h) > 0
              ? `${stats.succeededLast24h} ok · ${stats.failedLast24h} failed`
              : "No terminal runs yet"
          }
          loading={!stats}
        />
        <StatCard
          icon={<Clock className="h-4 w-4" />}
          label="Avg duration (24h)"
          value={stats?.avgDurationMsLast24h != null ? formatMs(stats.avgDurationMsLast24h) : "—"}
          hint={
            stats?.p95DurationMsLast24h != null
              ? `p50 ${formatMs(stats.p50DurationMsLast24h!)} · p95 ${formatMs(stats.p95DurationMsLast24h)}`
              : undefined
          }
          loading={!stats}
        />
      </div>

      <section className="rounded-lg border bg-card p-6">
        <div className="mb-4 flex flex-wrap items-center justify-between gap-3">
          <div className="flex items-center gap-2">
            <Activity className="h-4 w-4 text-muted-foreground" />
            <h2 className="text-sm font-medium">Executions over time</h2>
          </div>
          <div className="inline-flex items-center gap-1 rounded-md border bg-background p-0.5 text-xs">
            {RANGES.map((r) => (
              <button
                key={r}
                type="button"
                onClick={() => setRange(r)}
                className={
                  r === range
                    ? "rounded-sm bg-card px-2.5 py-1 font-medium text-foreground shadow-sm"
                    : "rounded-sm px-2.5 py-1 text-muted-foreground hover:text-foreground"
                }
              >
                {r === "24h" ? "Last 24h" : "Last 7d"}
              </button>
            ))}
          </div>
        </div>

        {series ? (
          <ExecutionsChart points={series.points} range={series.range} />
        ) : error ? (
          <div className="flex h-[260px] items-center justify-center text-xs text-muted-foreground">
            Failed to load chart.
          </div>
        ) : (
          <div className="flex h-[260px] animate-pulse items-center justify-center rounded-md bg-muted/20" />
        )}

        <div className="mt-4 flex items-center gap-6 text-xs text-muted-foreground">
          <span className="inline-flex items-center gap-1.5">
            <span className="h-2 w-2 rounded-full bg-emerald-400" />
            Succeeded
          </span>
          <span className="inline-flex items-center gap-1.5">
            <span className="h-2 w-2 rounded-full bg-destructive" />
            Failed
          </span>
        </div>
      </section>

      {stats && stats.workflowsTotal === 0 && (
        <EmptyState
          icon={<Workflow className="h-4 w-4" />}
          title="No workflows yet"
          description="Create your first workflow to start automating with webhooks, schedules, HTTP calls and more."
          action={
            <Button asChild>
              <Link href="/workflows/new">
                <Zap className="h-3.5 w-3.5" />
                Create workflow
              </Link>
            </Button>
          }
        />
      )}

      {stats && stats.workflowsTotal > 0 && stats.runs7d === 0 && (
        <div className="rounded-lg border border-dashed bg-card/30 p-6 text-sm text-muted-foreground">
          <div className="flex items-center gap-2 text-foreground">
            <XCircle className="h-4 w-4 text-muted-foreground" />
            <span className="font-medium">No executions in the last 7 days.</span>
          </div>
          <p className="mt-1.5">
            Trigger one manually from any workflow, or POST to your webhook URL to see runs appear here.
          </p>
        </div>
      )}
    </div>
  );
}

function formatMs(ms: number): string {
  if (ms < 1000) return `${ms}ms`;
  const sec = ms / 1000;
  if (sec < 60) return `${sec.toFixed(sec < 10 ? 2 : 1)}s`;
  const min = sec / 60;
  return `${min.toFixed(1)}m`;
}
