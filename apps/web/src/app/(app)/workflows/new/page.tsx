"use client";

import Link from "next/link";
import { ChevronLeft } from "lucide-react";
import { WorkflowForm } from "@/components/workflow/workflow-form";

export default function NewWorkflowPage() {
  return (
    <div className="space-y-8">
      <header>
        <Link
          href="/workflows"
          className="inline-flex items-center gap-1 text-xs text-muted-foreground hover:text-foreground"
        >
          <ChevronLeft className="h-3 w-3" />
          Back to workflows
        </Link>
        <h1 className="mt-3 text-2xl font-semibold tracking-tight">New workflow</h1>
        <p className="mt-1 text-sm text-muted-foreground">
          Configure the trigger and chain together the actions that should run.
        </p>
      </header>

      <WorkflowForm />
    </div>
  );
}
