"use client";

import { useCallback, useEffect, useState } from "react";
import Link from "next/link";
import { Plus, Workflow as WorkflowIcon } from "lucide-react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { ListSkeleton } from "@/components/ui/skeleton";
import { EmptyState } from "@/components/shared/empty-state";
import { WorkflowCard } from "@/components/workflow/workflow-card";
import { workflowsApi } from "@/lib/workflows/api";
import type { WorkflowSummary } from "@/lib/workflows/types";

export default function WorkflowsPage() {
  const [items, setItems] = useState<WorkflowSummary[] | null>(null);

  const load = useCallback(async () => {
    try {
      const list = await workflowsApi.list();
      setItems(list);
    } catch {
      toast.error("Could not load workflows.");
      setItems([]);
    }
  }, []);

  useEffect(() => {
    void load();
  }, [load]);

  return (
    <div className="space-y-8">
      <header className="flex items-end justify-between gap-4">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight">Workflows</h1>
          <p className="mt-1 text-sm text-muted-foreground">
            Automate work across services. Webhooks, schedules, HTTP, and more.
          </p>
        </div>
        <Button asChild>
          <Link href="/workflows/new">
            <Plus className="h-3.5 w-3.5" />
            New workflow
          </Link>
        </Button>
      </header>

      {items === null ? (
        <ListSkeleton rows={3} />
      ) : items.length === 0 ? (
        <EmptyState
          icon={<WorkflowIcon className="h-4 w-4" />}
          title="No workflows yet"
          description="Create your first workflow to trigger actions from webhooks or schedules."
          action={
            <Button asChild>
              <Link href="/workflows/new">
                <Plus className="h-3.5 w-3.5" />
                Create workflow
              </Link>
            </Button>
          }
        />
      ) : (
        <ul className="space-y-2">
          {items.map((w) => (
            <li key={w.id}>
              <WorkflowCard workflow={w} onChange={load} />
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
