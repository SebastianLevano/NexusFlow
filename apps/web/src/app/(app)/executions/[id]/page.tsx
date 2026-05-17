"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import Link from "next/link";
import { useParams } from "next/navigation";
import { ChevronLeft, Loader2, Radio } from "lucide-react";
import { toast } from "sonner";
import { Badge } from "@/components/ui/badge";
import { StepTimeline } from "@/components/execution/step-timeline";
import { executionsApi } from "@/lib/executions/api";
import { formatDuration, formatRelative, statusVariant } from "@/lib/executions/format";
import {
  STATUS_LABELS,
  type Execution,
  type ExecutionStatus,
  type StepExecution,
  type StepStatus,
} from "@/lib/executions/types";
import {
  buildExecutionHub,
  HubConnectionState,
  joinExecution,
  leaveExecution,
  type ExecutionLiveEvent,
  type StepLiveEvent,
} from "@/lib/signalr/execution-hub";

function isTerminal(status: ExecutionStatus): boolean {
  return status === "succeeded" || status === "failed";
}

function tryParseJson(raw: string | null): Record<string, unknown> {
  if (!raw) return {};
  try {
    const parsed = JSON.parse(raw);
    return parsed && typeof parsed === "object" ? (parsed as Record<string, unknown>) : {};
  } catch {
    return {};
  }
}

export default function ExecutionDetailPage() {
  const { id } = useParams<{ id: string }>();
  const [execution, setExecution] = useState<Execution | null>(null);
  const [notFound, setNotFound] = useState(false);
  const [live, setLive] = useState(false);
  const liveRef = useRef(false);

  const load = useCallback(async () => {
    try {
      setExecution(await executionsApi.get(id));
    } catch (err: unknown) {
      const status = (err as { response?: { status?: number } }).response?.status;
      if (status === 404) setNotFound(true);
      else toast.error("Could not load execution.");
    }
  }, [id]);

  useEffect(() => {
    void load();
  }, [load]);

  // SignalR live updates
  useEffect(() => {
    let cancelled = false;
    let joined = false;
    const connection = buildExecutionHub({
      onExecution: (evt: ExecutionLiveEvent) => {
        if (evt.executionId !== id) return;
        setExecution((prev) => {
          if (!prev) return prev;
          return {
            ...prev,
            status: evt.status,
            durationMs: evt.durationMs ?? prev.durationMs,
            errorMessage: evt.errorMessage ?? prev.errorMessage,
            startedAt: prev.startedAt ?? (evt.status === "running" ? evt.at : prev.startedAt),
            finishedAt: isTerminal(evt.status) ? evt.at : prev.finishedAt,
          };
        });
      },
      onStep: (evt: StepLiveEvent) => {
        if (evt.executionId !== id) return;
        setExecution((prev) => {
          if (!prev) return prev;
          const idx = prev.steps.findIndex((s) => s.id === evt.stepId);
          const stepPatch: Partial<StepExecution> = {
            status: evt.status as StepStatus,
            error: evt.error ?? null,
            durationMs: evt.durationMs ?? null,
          };
          if (evt.inputJson) stepPatch.input = tryParseJson(evt.inputJson);
          if (evt.outputJson) stepPatch.output = tryParseJson(evt.outputJson);

          let steps: StepExecution[];
          if (idx >= 0) {
            steps = prev.steps.map((s, i) => (i === idx ? { ...s, ...stepPatch } : s));
          } else {
            const fresh: StepExecution = {
              id: evt.stepId,
              stepId: evt.stepId,
              orderIndex: evt.orderIndex,
              actionType: evt.actionType,
              status: evt.status as StepStatus,
              input: tryParseJson(evt.inputJson),
              output: tryParseJson(evt.outputJson),
              error: evt.error ?? null,
              startedAt: evt.status === "running" ? evt.at : null,
              finishedAt: evt.status === "succeeded" || evt.status === "failed" ? evt.at : null,
              durationMs: evt.durationMs ?? null,
            };
            steps = [...prev.steps, fresh].sort((a, b) => a.orderIndex - b.orderIndex);
          }
          return { ...prev, steps };
        });
      },
      onStatusChange: (state) => {
        const connected = state === HubConnectionState.Connected;
        liveRef.current = connected;
        setLive(connected);
      },
    });

    (async () => {
      try {
        await connection.start();
        if (cancelled) {
          await connection.stop();
          return;
        }
        joined = await joinExecution(connection, id);
        liveRef.current = joined;
        setLive(joined);
      } catch {
        liveRef.current = false;
        setLive(false);
      }
    })();

    return () => {
      cancelled = true;
      (async () => {
        try {
          if (joined) await leaveExecution(connection, id);
          await connection.stop();
        } catch {
          // ignore
        }
      })();
    };
  }, [id]);

  // Polling fallback while not connected and execution still running
  useEffect(() => {
    if (!execution) return;
    if (isTerminal(execution.status)) return;
    const t = window.setInterval(() => {
      if (!liveRef.current) void load();
    }, 1500);
    return () => window.clearInterval(t);
  }, [execution, load]);

  if (notFound) {
    return (
      <div className="space-y-4">
        <Link href="/executions" className="inline-flex items-center gap-1 text-xs text-muted-foreground hover:text-foreground">
          <ChevronLeft className="h-3 w-3" /> Back to executions
        </Link>
        <h1 className="text-xl font-semibold">Execution not found</h1>
      </div>
    );
  }

  if (!execution) {
    return (
      <div className="flex items-center justify-center py-16">
        <Loader2 className="h-4 w-4 animate-spin text-muted-foreground" />
      </div>
    );
  }

  const runningOrPending = !isTerminal(execution.status);

  return (
    <div className="space-y-8">
      <header>
        <Link href="/executions" className="inline-flex items-center gap-1 text-xs text-muted-foreground hover:text-foreground">
          <ChevronLeft className="h-3 w-3" /> Back to executions
        </Link>

        <div className="mt-3 flex flex-wrap items-center gap-3">
          <h1 className="text-2xl font-semibold tracking-tight">Execution</h1>
          <Badge variant={statusVariant(execution.status)}>{STATUS_LABELS[execution.status]}</Badge>
          {runningOrPending && (
            <span
              className={
                live
                  ? "inline-flex items-center gap-1.5 rounded-md border border-emerald-500/30 bg-emerald-500/10 px-2 py-0.5 text-xs text-emerald-400"
                  : "inline-flex items-center gap-1.5 rounded-md border bg-muted px-2 py-0.5 text-xs text-muted-foreground"
              }
            >
              <Radio className={live ? "h-3 w-3 animate-pulse" : "h-3 w-3"} />
              {live ? "Live" : "Polling"}
            </span>
          )}
        </div>

        <dl className="mt-4 grid grid-cols-2 gap-4 sm:grid-cols-4">
          <Stat label="Workflow">
            <Link href={`/workflows/${execution.workflowId}`} className="hover:underline">
              View workflow
            </Link>
          </Stat>
          <Stat label="Triggered by">{execution.triggeredBy}</Stat>
          <Stat label="Duration">{formatDuration(execution.durationMs)}</Stat>
          <Stat label="Started">{formatRelative(execution.startedAt ?? execution.createdAt)}</Stat>
        </dl>
      </header>

      {execution.errorMessage && (
        <div className="rounded-md border border-destructive/30 bg-destructive/5 px-4 py-3 text-sm text-destructive">
          {execution.errorMessage}
        </div>
      )}

      <section>
        <h2 className="mb-3 text-sm font-medium">Steps</h2>
        <StepTimeline steps={execution.steps} />
      </section>

      <section className="rounded-lg border bg-card p-6">
        <h2 className="mb-3 text-sm font-medium">Trigger payload</h2>
        <pre className="max-h-72 overflow-auto rounded-md border bg-background px-3 py-2 font-mono text-xs">
          {JSON.stringify(execution.triggerPayload, null, 2)}
        </pre>
      </section>
    </div>
  );
}

function Stat({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <div>
      <dt className="text-xs uppercase tracking-wide text-muted-foreground">{label}</dt>
      <dd className="mt-1 text-sm">{children}</dd>
    </div>
  );
}
