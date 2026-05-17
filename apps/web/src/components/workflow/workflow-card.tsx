"use client";

import Link from "next/link";
import { useState } from "react";
import { Loader2, MoreHorizontal, Power, Trash2, Webhook, Clock } from "lucide-react";
import { toast } from "sonner";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { workflowsApi } from "@/lib/workflows/api";
import { TRIGGER_LABELS, type WorkflowSummary } from "@/lib/workflows/types";
import { cn } from "@/lib/utils";

interface Props {
  workflow: WorkflowSummary;
  onChange: () => void;
}

export function WorkflowCard({ workflow, onChange }: Props) {
  const [busy, setBusy] = useState<"toggle" | "delete" | null>(null);

  async function handleToggle() {
    setBusy("toggle");
    try {
      if (workflow.isActive) await workflowsApi.deactivate(workflow.id);
      else await workflowsApi.activate(workflow.id);
      onChange();
    } catch {
      toast.error("Could not toggle workflow.");
    } finally {
      setBusy(null);
    }
  }

  async function handleDelete() {
    if (!confirm(`Delete "${workflow.name}"? This cannot be undone.`)) return;
    setBusy("delete");
    try {
      await workflowsApi.remove(workflow.id);
      toast.success("Workflow deleted.");
      onChange();
    } catch {
      toast.error("Could not delete workflow.");
    } finally {
      setBusy(null);
    }
  }

  const TriggerIcon = workflow.triggerType === "schedule" ? Clock : Webhook;

  return (
    <div className="group flex items-center justify-between gap-4 rounded-lg border bg-card p-4 transition-colors hover:border-border/80">
      <Link href={`/workflows/${workflow.id}`} className="flex min-w-0 flex-1 items-center gap-3">
        <div className="flex h-9 w-9 shrink-0 items-center justify-center rounded-md border bg-background text-muted-foreground">
          <TriggerIcon className="h-4 w-4" />
        </div>
        <div className="min-w-0">
          <div className="flex items-center gap-2">
            <p className="truncate text-sm font-medium">{workflow.name}</p>
            <Badge variant={workflow.isActive ? "active" : "inactive"}>
              <span
                className={cn(
                  "h-1.5 w-1.5 rounded-full",
                  workflow.isActive ? "bg-emerald-400" : "bg-muted-foreground/40",
                )}
              />
              {workflow.isActive ? "Active" : "Inactive"}
            </Badge>
          </div>
          <p className="mt-0.5 truncate text-xs text-muted-foreground">
            {TRIGGER_LABELS[workflow.triggerType]} · {workflow.stepCount}{" "}
            {workflow.stepCount === 1 ? "step" : "steps"}
            {workflow.description ? ` · ${workflow.description}` : ""}
          </p>
        </div>
      </Link>

      <div className="flex items-center gap-1 opacity-0 transition-opacity group-hover:opacity-100">
        <Button
          variant="ghost"
          size="sm"
          onClick={handleToggle}
          disabled={busy !== null}
          aria-label={workflow.isActive ? "Deactivate" : "Activate"}
        >
          {busy === "toggle" ? <Loader2 className="h-3.5 w-3.5 animate-spin" /> : <Power className="h-3.5 w-3.5" />}
        </Button>
        <Button
          variant="ghost"
          size="sm"
          onClick={handleDelete}
          disabled={busy !== null}
          aria-label="Delete"
        >
          {busy === "delete" ? <Loader2 className="h-3.5 w-3.5 animate-spin" /> : <Trash2 className="h-3.5 w-3.5" />}
        </Button>
        <Button asChild variant="ghost" size="sm" aria-label="Open">
          <Link href={`/workflows/${workflow.id}`}>
            <MoreHorizontal className="h-3.5 w-3.5" />
          </Link>
        </Button>
      </div>
    </div>
  );
}
