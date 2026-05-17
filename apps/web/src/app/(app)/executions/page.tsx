"use client";

import { Suspense, useCallback, useEffect, useState } from "react";
import Link from "next/link";
import { useSearchParams } from "next/navigation";
import { CheckCircle2, Clock, History, Loader2, XCircle, Zap } from "lucide-react";
import { toast } from "sonner";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { ListSkeleton } from "@/components/ui/skeleton";
import { EmptyState } from "@/components/shared/empty-state";
import { FiltersBar } from "@/components/execution/filters-bar";
import { executionsApi } from "@/lib/executions/api";
import { formatDuration, formatRelative, statusVariant } from "@/lib/executions/format";
import {
  STATUS_LABELS,
  type ExecutionStatus,
  type ExecutionSummary,
  type ExecutionsListFilters,
} from "@/lib/executions/types";

export default function ExecutionsPage() {
  return (
    <Suspense
      fallback={
        <div className="flex items-center justify-center py-16">
          <Loader2 className="h-4 w-4 animate-spin text-muted-foreground" />
        </div>
      }
    >
      <ExecutionsView />
    </Suspense>
  );
}

function ExecutionsView() {
  const params = useSearchParams();
  const workflowId = params.get("workflowId") || undefined;
  const status = (params.get("status") as ExecutionStatus | null) || undefined;
  const range = (params.get("range") as ExecutionsListFilters["range"]) || undefined;

  const [items, setItems] = useState<ExecutionSummary[] | null>(null);

  const load = useCallback(async () => {
    try {
      setItems(await executionsApi.list({ workflowId, status, range }));
    } catch {
      toast.error("Could not load executions.");
      setItems([]);
    }
  }, [workflowId, status, range]);

  useEffect(() => {
    setItems(null);
    void load();
    const t = window.setInterval(() => void load(), 5000);
    return () => window.clearInterval(t);
  }, [load]);

  const hasFilters = Boolean(workflowId || status || range);

  return (
    <div className="space-y-8">
      <header>
        <h1 className="text-2xl font-semibold tracking-tight">Executions</h1>
        <p className="mt-1 text-sm text-muted-foreground">
          Every workflow run, with status and duration. Auto-refreshes every 5 seconds.
        </p>
      </header>

      <FiltersBar />

      {items === null ? (
        <ListSkeleton rows={5} />
      ) : items.length === 0 ? (
        hasFilters ? (
          <EmptyState
            icon={<History className="h-4 w-4" />}
            title="No executions match these filters"
            description="Try widening the time range, removing the status filter, or selecting a different workflow."
          />
        ) : (
          <EmptyState
            icon={<History className="h-4 w-4" />}
            title="No executions yet"
            description="Trigger a workflow manually, via webhook, or wait for a schedule to fire."
            action={
              <Button asChild>
                <Link href="/workflows">
                  <Zap className="h-3.5 w-3.5" />
                  Go to workflows
                </Link>
              </Button>
            }
          />
        )
      ) : (
        <ul className="space-y-2">
          {items.map((e) => (
            <li key={e.id}>
              <ExecutionRow execution={e} />
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}

function ExecutionRow({ execution }: { execution: ExecutionSummary }) {
  return (
    <Link
      href={`/executions/${execution.id}`}
      className="flex items-center gap-4 rounded-lg border bg-card p-4 transition-colors hover:border-border/80"
    >
      <StatusIcon status={execution.status} />
      <div className="min-w-0 flex-1">
        <div className="flex items-center gap-2">
          <p className="truncate text-sm font-medium">
            Run via <span className="text-muted-foreground">{execution.triggeredBy}</span>
          </p>
          <Badge variant={statusVariant(execution.status)}>{STATUS_LABELS[execution.status]}</Badge>
        </div>
        <p className="mt-0.5 truncate text-xs text-muted-foreground">
          {execution.stepCount} {execution.stepCount === 1 ? "step" : "steps"} ·{" "}
          {formatDuration(execution.durationMs)} · {formatRelative(execution.createdAt)}
          {execution.errorMessage ? ` · ${execution.errorMessage}` : ""}
        </p>
      </div>
    </Link>
  );
}

function StatusIcon({ status }: { status: ExecutionStatus }) {
  if (status === "succeeded")
    return (
      <div className="flex h-9 w-9 items-center justify-center rounded-md border bg-emerald-500/10 text-emerald-400">
        <CheckCircle2 className="h-4 w-4" />
      </div>
    );
  if (status === "failed")
    return (
      <div className="flex h-9 w-9 items-center justify-center rounded-md border bg-destructive/10 text-destructive">
        <XCircle className="h-4 w-4" />
      </div>
    );
  if (status === "running")
    return (
      <div className="flex h-9 w-9 items-center justify-center rounded-md border bg-primary/10 text-primary">
        <Loader2 className="h-4 w-4 animate-spin" />
      </div>
    );
  return (
    <div className="flex h-9 w-9 items-center justify-center rounded-md border bg-muted text-muted-foreground">
      <Clock className="h-4 w-4" />
    </div>
  );
}
