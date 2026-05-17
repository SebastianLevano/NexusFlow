"use client";

import { useState } from "react";
import { CheckCircle2, ChevronDown, ChevronRight, Clock, Loader2, XCircle } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { ACTION_LABELS, type ActionType } from "@/lib/workflows/types";
import { formatDuration } from "@/lib/executions/format";
import type { StepExecution, StepStatus } from "@/lib/executions/types";
import { cn } from "@/lib/utils";

interface Props {
  steps: StepExecution[];
}

export function StepTimeline({ steps }: Props) {
  if (steps.length === 0) {
    return (
      <p className="rounded-md border border-dashed bg-card/30 px-4 py-6 text-center text-sm text-muted-foreground">
        This execution had no steps to run.
      </p>
    );
  }

  return (
    <ol className="space-y-2">
      {steps.map((step) => (
        <li key={step.id}>
          <StepRow step={step} />
        </li>
      ))}
    </ol>
  );
}

function StepRow({ step }: { step: StepExecution }) {
  const [open, setOpen] = useState(false);
  const actionLabel = ACTION_LABELS[step.actionType as ActionType] ?? step.actionType;

  return (
    <div className="rounded-lg border bg-card">
      <button
        type="button"
        onClick={() => setOpen((v) => !v)}
        className="flex w-full items-center gap-3 px-4 py-3 text-left"
      >
        <StatusBubble status={step.status} index={step.orderIndex + 1} />
        <div className="min-w-0 flex-1">
          <div className="flex items-center gap-2">
            <p className="truncate text-sm font-medium">{actionLabel}</p>
            <StatusBadge status={step.status} />
          </div>
          <p className="mt-0.5 truncate text-xs text-muted-foreground">
            {formatDuration(step.durationMs)}
            {step.error ? ` · ${step.error}` : ""}
          </p>
        </div>
        {open ? (
          <ChevronDown className="h-4 w-4 text-muted-foreground" />
        ) : (
          <ChevronRight className="h-4 w-4 text-muted-foreground" />
        )}
      </button>

      {open && (
        <div className="space-y-3 border-t bg-background/40 px-4 py-3">
          <JsonBlock label="Input" value={step.input} />
          <JsonBlock label="Output" value={step.output} />
          {step.error && (
            <div>
              <p className="mb-1 text-xs font-medium uppercase tracking-wide text-muted-foreground">Error</p>
              <p className="rounded-md border border-destructive/30 bg-destructive/5 px-3 py-2 font-mono text-xs text-destructive">
                {step.error}
              </p>
            </div>
          )}
        </div>
      )}
    </div>
  );
}

function StatusBubble({ status, index }: { status: StepStatus; index: number }) {
  const base = "flex h-7 w-7 shrink-0 items-center justify-center rounded-full border text-xs font-medium";
  if (status === "succeeded")
    return (
      <div className={cn(base, "border-emerald-500/30 bg-emerald-500/10 text-emerald-400")}>
        <CheckCircle2 className="h-3.5 w-3.5" />
      </div>
    );
  if (status === "failed")
    return (
      <div className={cn(base, "border-destructive/30 bg-destructive/10 text-destructive")}>
        <XCircle className="h-3.5 w-3.5" />
      </div>
    );
  if (status === "running")
    return (
      <div className={cn(base, "border-primary/30 bg-primary/10 text-primary")}>
        <Loader2 className="h-3.5 w-3.5 animate-spin" />
      </div>
    );
  if (status === "pending")
    return (
      <div className={cn(base, "bg-muted text-muted-foreground")}>
        <Clock className="h-3.5 w-3.5" />
      </div>
    );
  return <div className={cn(base, "bg-background text-muted-foreground")}>{index}</div>;
}

function StatusBadge({ status }: { status: StepStatus }) {
  const labelMap: Record<StepStatus, string> = {
    succeeded: "Succeeded",
    failed: "Failed",
    running: "Running",
    pending: "Pending",
    skipped: "Skipped",
  };
  const variant = status === "succeeded" ? "active" : status === "running" ? "accent" : "inactive";
  return <Badge variant={variant}>{labelMap[status]}</Badge>;
}

function JsonBlock({ label, value }: { label: string; value: unknown }) {
  const json = JSON.stringify(value, null, 2);
  return (
    <div>
      <p className="mb-1 text-xs font-medium uppercase tracking-wide text-muted-foreground">{label}</p>
      <pre className="max-h-64 overflow-auto rounded-md border bg-background px-3 py-2 font-mono text-xs">{json}</pre>
    </div>
  );
}
