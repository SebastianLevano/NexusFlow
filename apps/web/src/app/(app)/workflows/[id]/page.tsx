"use client";

import { useCallback, useEffect, useState } from "react";
import Link from "next/link";
import { useParams, useRouter } from "next/navigation";
import { ChevronLeft, History, Loader2, Play, Power, Trash2 } from "lucide-react";
import { toast } from "sonner";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { WorkflowForm } from "@/components/workflow/workflow-form";
import { WebhookInfo } from "@/components/workflow/webhook-info";
import { workflowsApi } from "@/lib/workflows/api";
import { executionsApi } from "@/lib/executions/api";
import type { Workflow } from "@/lib/workflows/types";

export default function WorkflowDetailPage() {
  const { id } = useParams<{ id: string }>();
  const router = useRouter();
  const [workflow, setWorkflow] = useState<Workflow | null>(null);
  const [notFound, setNotFound] = useState(false);
  const [busy, setBusy] = useState<"toggle" | "delete" | "run" | null>(null);

  const load = useCallback(async () => {
    try {
      setWorkflow(await workflowsApi.get(id));
    } catch (err: unknown) {
      const status = (err as { response?: { status?: number } }).response?.status;
      if (status === 404) setNotFound(true);
      else toast.error("Could not load workflow.");
    }
  }, [id]);

  useEffect(() => {
    void load();
  }, [load]);

  async function toggleActive() {
    if (!workflow) return;
    setBusy("toggle");
    try {
      const next = workflow.isActive
        ? await workflowsApi.deactivate(workflow.id)
        : await workflowsApi.activate(workflow.id);
      setWorkflow(next);
    } catch {
      toast.error("Could not toggle workflow.");
    } finally {
      setBusy(null);
    }
  }

  async function handleDelete() {
    if (!workflow) return;
    if (!confirm(`Delete "${workflow.name}"? This cannot be undone.`)) return;
    setBusy("delete");
    try {
      await workflowsApi.remove(workflow.id);
      toast.success("Workflow deleted.");
      router.replace("/workflows");
    } catch {
      toast.error("Could not delete workflow.");
      setBusy(null);
    }
  }

  async function handleRun() {
    if (!workflow) return;
    setBusy("run");
    try {
      const { executionId } = await executionsApi.runManual(workflow.id);
      toast.success("Execution queued.");
      router.push(`/executions/${executionId}`);
    } catch {
      toast.error("Could not start execution.");
      setBusy(null);
    }
  }

  if (notFound) {
    return (
      <div className="space-y-4">
        <Link href="/workflows" className="inline-flex items-center gap-1 text-xs text-muted-foreground hover:text-foreground">
          <ChevronLeft className="h-3 w-3" /> Back to workflows
        </Link>
        <h1 className="text-xl font-semibold">Workflow not found</h1>
        <p className="text-sm text-muted-foreground">It may have been deleted or you don&apos;t have access.</p>
      </div>
    );
  }

  if (!workflow) {
    return (
      <div className="flex items-center justify-center py-16">
        <Loader2 className="h-4 w-4 animate-spin text-muted-foreground" />
      </div>
    );
  }

  const webhookSecret =
    workflow.triggerType === "webhook" && typeof workflow.triggerConfig?.secret === "string"
      ? (workflow.triggerConfig.secret as string)
      : null;

  return (
    <div className="space-y-8">
      <header className="flex flex-wrap items-end justify-between gap-4">
        <div>
          <Link href="/workflows" className="inline-flex items-center gap-1 text-xs text-muted-foreground hover:text-foreground">
            <ChevronLeft className="h-3 w-3" /> Back to workflows
          </Link>
          <div className="mt-3 flex items-center gap-2">
            <h1 className="text-2xl font-semibold tracking-tight">{workflow.name}</h1>
            <Badge variant={workflow.isActive ? "active" : "inactive"}>
              <span
                className={
                  workflow.isActive
                    ? "h-1.5 w-1.5 rounded-full bg-emerald-400"
                    : "h-1.5 w-1.5 rounded-full bg-muted-foreground/40"
                }
              />
              {workflow.isActive ? "Active" : "Inactive"}
            </Badge>
          </div>
          <p className="mt-1 text-sm text-muted-foreground">{workflow.description ?? "No description."}</p>
        </div>
        <div className="flex flex-wrap items-center gap-2">
          <Button onClick={handleRun} disabled={busy !== null}>
            {busy === "run" ? <Loader2 className="h-3.5 w-3.5 animate-spin" /> : <Play className="h-3.5 w-3.5" />}
            Run now
          </Button>
          <Button asChild variant="outline">
            <Link href={`/executions?workflowId=${workflow.id}`}>
              <History className="h-3.5 w-3.5" />
              View runs
            </Link>
          </Button>
          <Button variant="outline" onClick={toggleActive} disabled={busy !== null}>
            {busy === "toggle" ? <Loader2 className="h-3.5 w-3.5 animate-spin" /> : <Power className="h-3.5 w-3.5" />}
            {workflow.isActive ? "Deactivate" : "Activate"}
          </Button>
          <Button variant="ghost" onClick={handleDelete} disabled={busy !== null}>
            {busy === "delete" ? <Loader2 className="h-3.5 w-3.5 animate-spin" /> : <Trash2 className="h-3.5 w-3.5" />}
            Delete
          </Button>
        </div>
      </header>

      {workflow.triggerType === "webhook" && (
        <WebhookInfo workflowId={workflow.id} secret={webhookSecret} />
      )}

      <WorkflowForm workflow={workflow} />
    </div>
  );
}
